using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// Server-authoritative chase AI driven by shared flow fields; attack execution lives in EnemyActions.
public class EnemyAI : NetworkBehaviour
{
    private EnemyKit kit;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private NetworkTransform networkTransform;
    private WaveSpawner waveSpawner;
    private EnemyActions actions;
    private Enemy hub;
    private float bodyRadius;
    private Vector2 bodyOffset;
    private float nextAttackTime;
    private float nextLeashCheckTime;
    private float knockbackEndTime;
    private Vector2 knockbackVelocity;

    private Player target;
    private int seenPlayersVersion = -1;
    private float nextRetargetTime;
    private float nextSteerTime;
    private float nextLineOfSightTime;
    private bool hasLineOfSight;
    private Vector2 steerDirection;

    private Vector2 lastPosition;
    private float lastMovedTime;
    private float attackVisualEndTime;
    private int currentAnimationHash;

    private static PhysicsMaterial2D slipperyMaterial;

    // Resolved per-kit in ApplyVisual, since Aseprite-generated controllers don't all share the same state names.
    private int idleHash;
    private int runHash;
    private int attackHash;

    // Assigned at spawn time by whoever instantiates this enemy (see Enemy.Initialize); never baked into the prefab.
    public EnemyKit Kit { get => kit; set => kit = value; }

    // Replicates which EnemyKit to use, so remote clients (skipped by WaveSpawner's direct Initialize call) can resolve it too.
    private readonly NetworkVariable<int> kitIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.sharedMaterial = GetSlipperyMaterial();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        networkTransform = GetComponent<NetworkTransform>();
        actions = GetComponent<EnemyActions>();
        hub = GetComponent<Enemy>();

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        bodyRadius = circle != null ? circle.radius * scale : 0.4f;
        bodyOffset = circle != null ? circle.offset * (Vector2)transform.lossyScale : Vector2.zero;
        EnemyFlowFieldManager.BodyOffset = bodyOffset;
    }

    // Staggers timers so enemies don't all update the same frame; safe to skip on a remote client, server-only anyway.
    void Start()
    {
        lastPosition = transform.position;
        waveSpawner = FindAnyObjectByType<WaveSpawner>();

        if (kit == null)
            return;

        nextRetargetTime = Time.time + Random.value * kit.RetargetInterval;
        nextSteerTime = Time.time + Random.value * kit.SteerInterval;
        nextLineOfSightTime = Time.time + Random.value * kit.LineOfSightInterval;
        nextLeashCheckTime = Time.time + Random.value * kit.LeashCheckInterval;
    }

    public override void OnNetworkSpawn()
    {
        kitIndex.OnValueChanged += HandleKitIndexChanged;
        if (kit == null && kitIndex.Value >= 0)
            ApplyKitFromIndex(kitIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        kitIndex.OnValueChanged -= HandleKitIndexChanged;
    }

    // Called by WaveSpawner before Spawn(), so the index is part of the payload remote clients first see.
    public void SetNetworkedKitIndex(int index) => kitIndex.Value = index;

    private void HandleKitIndexChanged(int previous, int current)
    {
        if (kit == null && current >= 0)
            ApplyKitFromIndex(current);
    }

    private void ApplyKitFromIndex(int index)
    {
        WaveSpawner spawner = waveSpawner != null ? waveSpawner : FindAnyObjectByType<WaveSpawner>();
        EnemyKit resolvedKit = spawner != null ? spawner.GetKit(index) : null;
        if (resolvedKit != null)
            hub.Initialize(resolvedKit);
    }

    // Runs on every peer; facing and animation come from real movement.
    void Update()
    {
        UpdateVisuals();
    }

    // Server-only per-tick loop: retarget, leash-check, attack, or steer toward the target.
    void FixedUpdate()
    {
        if (!this.HasServerAuthority())
            return;

        if (Time.time < knockbackEndTime)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        EnemyFlowFieldManager manager = EnemyFlowFieldManager.Instance;
        if (target == null || target.IsDead || seenPlayersVersion != manager.PlayersVersion || Time.time >= nextRetargetTime)
            ChooseTarget(manager);

        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time >= nextLeashCheckTime)
        {
            nextLeashCheckTime = Time.time + kit.LeashCheckInterval;
            if (CheckLeash())
                return;
        }

        if (actions != null && actions.IsWindingUp)
        {
            rb.linearVelocity = steerDirection * kit.MoveSpeed * kit.AttackMoveSpeedMultiplier;
            return;
        }

        Vector2 toTarget = TargetCenter(target) - BodyCenter;
        if (toTarget.sqrMagnitude <= kit.AttackRange * kit.AttackRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + kit.AttackCooldown;
                actions?.StartAttack(target);
            }
            return;
        }

        if (Time.time >= nextSteerTime)
        {
            nextSteerTime = Time.time + kit.SteerInterval;
            steerDirection = ComputeSteerDirection(manager, toTarget);
        }

        rb.linearVelocity = steerDirection * kit.MoveSpeed;
    }

    // The collider is offset from the pivot; queries must start at the real body.
    public Vector2 BodyCenter => rb.position + bodyOffset;

    // Overrides steering for a short window, e.g. a shield push; server-only caller.
    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        knockbackVelocity = velocity;
        knockbackEndTime = Time.time + duration;
    }

    // Range checks use collider centres, since pivots sit at different heights.
    public static Vector2 TargetCenter(Player player)
    {
        Collider2D collider = player.GetComponent<Collider2D>();
        return collider != null ? (Vector2)collider.bounds.center : (Vector2)player.transform.position;
    }

    private void ChooseTarget(EnemyFlowFieldManager manager)
    {
        nextRetargetTime = Time.time + kit.RetargetInterval;
        seenPlayersVersion = manager.PlayersVersion;
        target = manager.FindNearestLivingPlayer(rb.position);
    }

    // Recycles enemies that end up far from every player (e.g. players regrouped elsewhere) back near the action.
    private bool CheckLeash()
    {
        if (waveSpawner == null || (TargetCenter(target) - BodyCenter).sqrMagnitude <= kit.MaxDistanceFromPlayers * kit.MaxDistanceFromPlayers)
            return false;

        Vector3 teleportPos = waveSpawner.FindSpawnPosition();
        rb.position = teleportPos;
        rb.linearVelocity = Vector2.zero;
        if (networkTransform != null && IsSpawned)
            networkTransform.Teleport(teleportPos, transform.rotation, transform.localScale);
        else
            transform.position = teleportPos;

        return true;
    }

    // Straight at the player with a clear line, otherwise along the flow field.
    private Vector2 ComputeSteerDirection(EnemyFlowFieldManager manager, Vector2 toTarget)
    {
        if (toTarget.sqrMagnitude <= kit.DirectChaseRange * kit.DirectChaseRange && HasClearLine(toTarget))
            return toTarget.normalized;

        if (manager.TryGetDirection(target, BodyCenter, out Vector2 fieldDirection))
            return fieldDirection;

        return toTarget.normalized;
    }

    // Cached so cast cost stays flat however many enemies are near.
    private bool HasClearLine(Vector2 toTarget)
    {
        if (Time.time >= nextLineOfSightTime)
        {
            nextLineOfSightTime = Time.time + kit.LineOfSightInterval;
            hasLineOfSight = ObstacleQuery.IsClear(BodyCenter, BodyCenter + toTarget, bodyRadius * 1.15f);
        }
        return hasLineOfSight;
    }

    // Zero friction so enemies slide along trunks.
    private static PhysicsMaterial2D GetSlipperyMaterial()
    {
        if (slipperyMaterial == null)
            slipperyMaterial = new PhysicsMaterial2D("EnemySlippery") { friction = 0f, bounciness = 0f };
        return slipperyMaterial;
    }

    // Sprite/Animator are baked into the kit's own prefab variant now, not swapped at runtime - this just resolves state-name hashes, since Aseprite-generated controllers don't all share the same state names.
    public void CacheAnimationHashes()
    {
        idleHash = Animator.StringToHash(kit.IdleStateName);
        runHash = Animator.StringToHash(kit.RunStateName);
        attackHash = Animator.StringToHash(kit.AttackStateName);
    }

    // Called by EnemyActions so the shared Animator/state tracking stays in one place.
    public void PlayAttackVisual(float duration)
    {
        if (animator == null)
            return;

        attackVisualEndTime = Time.time + duration;
        currentAnimationHash = attackHash;
        animator.Play(attackHash, 0, 0f);
    }

    // Faces movement and picks Attack/Run/Idle, playing only on state change.
    private void UpdateVisuals()
    {
        Vector2 position = transform.position;
        Vector2 delta = position - lastPosition;
        lastPosition = position;

        if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
        {
            bool faceLeft = delta.x < 0f;
            if (spriteRenderer.flipX != faceLeft)
                spriteRenderer.flipX = faceLeft;
        }

        if (animator == null)
            return;

        float minStep = 0.3f * Time.deltaTime;
        if (delta.sqrMagnitude > minStep * minStep)
            lastMovedTime = Time.time;

        int desired;
        if (Time.time < attackVisualEndTime)
            desired = attackHash;
        else
            desired = Time.time - lastMovedTime < 0.15f ? runHash : idleHash;

        if (desired == currentAnimationHash)
            return;

        currentAnimationHash = desired;
        animator.Play(desired, 0, 0f);
    }
}
