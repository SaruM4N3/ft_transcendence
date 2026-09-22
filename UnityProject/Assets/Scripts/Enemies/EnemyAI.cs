using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// Server-authoritative chase-and-melee AI driven by shared flow fields.
public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float attackAngle = 100f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackHitDelay = 0.3f;
    [SerializeField] private float attackAnimationDuration = 0.7f;
    [SerializeField] private float attackMoveSpeedMultiplier = 0.25f;
    [SerializeField] private float hitRangeTolerance = 1.5f;
    [SerializeField] private float retargetInterval = 0.25f;
    [SerializeField] private float steerInterval = 0.1f;
    [SerializeField] private float directChaseRange = 8f;
    [SerializeField] private float lineOfSightInterval = 0.3f;
    [SerializeField] private float maxDistanceFromPlayers = 40f;
    [SerializeField] private float leashCheckInterval = 2f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private NetworkTransform networkTransform;
    private WaveSpawner waveSpawner;
    private float bodyRadius;
    private Vector2 bodyOffset;
    private float nextAttackTime;
    private float nextLeashCheckTime;

    private PlayerStats target;
    private int seenPlayersVersion = -1;
    private float nextRetargetTime;
    private float nextSteerTime;
    private float nextLineOfSightTime;
    private bool hasLineOfSight;
    private Vector2 steerDirection;

    private PlayerStats swingTarget;
    private float pendingHitTime;

    private Vector2 lastPosition;
    private float lastMovedTime;
    private float attackAnimationEndTime;
    private int currentAnimationHash;

    private static PhysicsMaterial2D slipperyMaterial;

    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private readonly NetworkVariable<byte> swingCount = new NetworkVariable<byte>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Direction the current/last swing faced, in degrees - replicated so remote clients (who never run
    // StartAttack themselves) can reproduce the same telegraph the server is resolving against.
    private readonly NetworkVariable<float> swingAngleDegrees = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // BodyCenter frozen at the moment the windup started. The enemy can still drift a little during the
    // windup (attackMoveSpeedMultiplier), so resolving the hit against its live position would check a
    // different spot than the telegraph, which is drawn once at this same origin - anchor both to it.
    private readonly NetworkVariable<Vector2> swingOrigin = new NetworkVariable<Vector2>(
        Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.sharedMaterial = GetSlipperyMaterial();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        networkTransform = GetComponent<NetworkTransform>();

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        bodyRadius = circle != null ? circle.radius * scale : 0.4f;
        bodyOffset = circle != null ? circle.offset * (Vector2)transform.lossyScale : Vector2.zero;
        EnemyFlowFieldManager.BodyOffset = bodyOffset;
    }

    // Staggers timers so enemies don't all update on the same frame.
    void Start()
    {
        lastPosition = transform.position;
        nextRetargetTime = Time.time + Random.value * retargetInterval;
        nextSteerTime = Time.time + Random.value * steerInterval;
        nextLineOfSightTime = Time.time + Random.value * lineOfSightInterval;
        nextLeashCheckTime = Time.time + Random.value * leashCheckInterval;
        waveSpawner = FindAnyObjectByType<WaveSpawner>();
    }

    public override void OnNetworkSpawn()
    {
        swingCount.OnValueChanged += HandleSwingCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        swingCount.OnValueChanged -= HandleSwingCountChanged;
    }

    // Replays the swing on remote clients.
    private void HandleSwingCountChanged(byte previous, byte current)
    {
        if (!IsServer)
            PlayAttackAnimation();
    }

    // Runs on every peer; facing and animation come from real movement.
    void Update()
    {
        UpdateVisuals();
    }

    void FixedUpdate()
    {
        if (!this.HasServerAuthority())
            return;

        if (pendingHitTime > 0f && Time.time >= pendingHitTime)
            ResolveHit();

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
            nextLeashCheckTime = Time.time + leashCheckInterval;
            if (CheckLeash())
                return;
        }

        // Committed to a swing: barely move regardless of where the target goes, so the telegraph
        // (frozen at the enemy's position when the windup started) stays an accurate warning.
        if (pendingHitTime > 0f)
        {
            rb.linearVelocity = steerDirection * moveSpeed * attackMoveSpeedMultiplier;
            return;
        }

        Vector2 toTarget = TargetCenter(target) - BodyCenter;
        if (toTarget.sqrMagnitude <= attackRange * attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (Time.time >= nextAttackTime)
                StartAttack();
            return;
        }

        if (Time.time >= nextSteerTime)
        {
            nextSteerTime = Time.time + steerInterval;
            steerDirection = ComputeSteerDirection(manager, toTarget);
        }

        rb.linearVelocity = steerDirection * moveSpeed;
    }

    // The collider is offset from the pivot; queries must start at the real body.
    private Vector2 BodyCenter => rb.position + bodyOffset;

    // Range checks use collider centres, since pivots sit at different heights.
    private static Vector2 TargetCenter(PlayerStats player)
    {
        Collider2D collider = player.GetComponent<Collider2D>();
        return collider != null ? (Vector2)collider.bounds.center : (Vector2)player.transform.position;
    }

    private void ChooseTarget(EnemyFlowFieldManager manager)
    {
        nextRetargetTime = Time.time + retargetInterval;
        seenPlayersVersion = manager.PlayersVersion;
        target = manager.FindNearestLivingPlayer(rb.position);
    }

    // Recycles enemies that end up far from every player (e.g. players regrouped elsewhere) back near the action.
    private bool CheckLeash()
    {
        if (waveSpawner == null || (TargetCenter(target) - BodyCenter).sqrMagnitude <= maxDistanceFromPlayers * maxDistanceFromPlayers)
            return false;

        Vector3 teleportPos = waveSpawner.FindSpawnPosition();
        rb.position = teleportPos;
        rb.linearVelocity = Vector2.zero;
        // Teleport() requires real Netcode authority; offline enemies are never actually spawned.
        if (networkTransform != null && IsSpawned)
            networkTransform.Teleport(teleportPos, transform.rotation, transform.localScale);
        else
            transform.position = teleportPos;

        return true;
    }

    // Straight at the player with a clear line, otherwise along the flow field.
    private Vector2 ComputeSteerDirection(EnemyFlowFieldManager manager, Vector2 toTarget)
    {
        if (toTarget.sqrMagnitude <= directChaseRange * directChaseRange && HasClearLine(toTarget))
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
            nextLineOfSightTime = Time.time + lineOfSightInterval;
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

    // Damage lands attackHitDelay after the windup starts.
    private void StartAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        swingTarget = target;
        pendingHitTime = Time.time + attackHitDelay;

        Vector2 origin = BodyCenter;
        Vector2 direction = (TargetCenter(target) - origin).normalized;
        // Written unconditionally (not just IsSpawned): ResolveHit/PlayAttackAnimation read these back
        // through .Value in every case, including fully offline solo play with no NetworkObject at all.
        swingOrigin.Value = origin;
        swingAngleDegrees.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (IsSpawned)
            swingCount.Value = (byte)(swingCount.Value + 1);
        PlayAttackAnimation(origin, direction);
    }

    // Connects only if the victim is alive, still in reach, and still within the swing's facing cone.
    // Checked against swingOrigin/swingAngle (frozen at windup start), not the enemy's current position,
    // so this always agrees with the telegraph that was actually shown to players.
    private void ResolveHit()
    {
        pendingHitTime = 0f;
        if (swingTarget == null || swingTarget.IsDead)
            return;

        Vector2 toVictim = TargetCenter(swingTarget) - swingOrigin.Value;
        float reach = attackRange * hitRangeTolerance;
        if (toVictim.sqrMagnitude > reach * reach)
            return;

        Vector2 swingDirection = SwingDirection();
        if (Vector2.Angle(swingDirection, toVictim) > attackAngle * 0.5f)
            return;

        swingTarget.RequestDamage(damage);
    }

    private Vector2 SwingDirection()
    {
        float radians = swingAngleDegrees.Value * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    // Remote clients reach this via HandleSwingCountChanged, with no local windup state of their own to use.
    private void PlayAttackAnimation() => PlayAttackAnimation(swingOrigin.Value, SwingDirection());

    private void PlayAttackAnimation(Vector2 origin, Vector2 direction)
    {
        if (animator != null)
        {
            attackAnimationEndTime = Time.time + attackAnimationDuration;
            currentAnimationHash = AttackHash;
            animator.Play(AttackHash, 0, 0f);
        }

        AttackTelegraph.Show(origin, direction, attackAngle, attackRange * hitRangeTolerance, attackHitDelay);
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
        if (Time.time < attackAnimationEndTime)
            desired = AttackHash;
        else
            desired = Time.time - lastMovedTime < 0.15f ? RunHash : IdleHash;

        if (desired == currentAnimationHash)
            return;

        currentAnimationHash = desired;
        animator.Play(desired, 0, 0f);
    }
}
