using Unity.Netcode;
using UnityEngine;

// Server-authoritative chase-and-melee AI built for hundreds of instances: shared per-player flow fields
// route it around water and decor, it re-aggros when its target dies, and deals contact damage on arrival.
// Movement replicates to clients via NetworkTransform (Server authority).
public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackHitDelay = 0.3f;
    [SerializeField] private float attackAnimationDuration = 0.7f;
    [SerializeField] private float hitRangeTolerance = 1.25f;
    [SerializeField] private float retargetInterval = 0.25f;
    [SerializeField] private float steerInterval = 0.1f;
    [SerializeField] private float directChaseRange = 8f;
    [SerializeField] private float lineOfSightInterval = 0.3f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float bodyRadius;
    private Vector2 bodyOffset;
    private float nextAttackTime;

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

    // Counts swings so remote clients replay the attack animation; only written by the server while spawned.
    private readonly NetworkVariable<byte> swingCount = new NetworkVariable<byte>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.sharedMaterial = GetSlipperyMaterial();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        bodyRadius = circle != null ? circle.radius * scale : 0.4f;
        bodyOffset = circle != null ? circle.offset * (Vector2)transform.lossyScale : Vector2.zero;
        EnemyFlowFieldManager.BodyOffset = bodyOffset;
    }

    // Random phase offsets spread the per-enemy timers so hundreds of enemies never all work on the same frame.
    void Start()
    {
        lastPosition = transform.position;
        nextRetargetTime = Time.time + Random.value * retargetInterval;
        nextSteerTime = Time.time + Random.value * steerInterval;
        nextLineOfSightTime = Time.time + Random.value * lineOfSightInterval;
    }

    public override void OnNetworkSpawn()
    {
        swingCount.OnValueChanged += HandleSwingCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        swingCount.OnValueChanged -= HandleSwingCountChanged;
    }

    // Remote clients replay the swing; the server already started it locally in StartAttack.
    private void HandleSwingCountChanged(byte previous, byte current)
    {
        if (!IsServer)
            PlayAttackAnimation();
    }

    // Runs on every peer: facing and Idle/Run/Attack come from real movement, so no extra sync is needed.
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

        Vector2 toTarget = (Vector2)target.transform.position - rb.position;
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

    // The collider is offset from the pivot, so every obstacle query must start at the real body, not the root.
    private Vector2 BodyCenter => rb.position + bodyOffset;

    private void ChooseTarget(EnemyFlowFieldManager manager)
    {
        nextRetargetTime = Time.time + retargetInterval;
        seenPlayersVersion = manager.PlayersVersion;
        target = manager.FindNearestLivingPlayer(rb.position);
    }

    // Straight at the player when close with a clear line, otherwise downhill along the shared flow field.
    private Vector2 ComputeSteerDirection(EnemyFlowFieldManager manager, Vector2 toTarget)
    {
        if (toTarget.sqrMagnitude <= directChaseRange * directChaseRange && HasClearLine(toTarget))
            return toTarget.normalized;

        if (manager.TryGetDirection(target, BodyCenter, out Vector2 fieldDirection))
            return fieldDirection;

        return toTarget.normalized;
    }

    // Cached for lineOfSightInterval so the cast cost stays flat however many enemies are near the player.
    private bool HasClearLine(Vector2 toTarget)
    {
        if (Time.time >= nextLineOfSightTime)
        {
            nextLineOfSightTime = Time.time + lineOfSightInterval;
            hasLineOfSight = ObstacleQuery.IsClear(BodyCenter, BodyCenter + toTarget, bodyRadius * 1.15f);
        }
        return hasLineOfSight;
    }

    // Zero friction so pushing against a trunk slides along it instead of sticking.
    private static PhysicsMaterial2D GetSlipperyMaterial()
    {
        if (slipperyMaterial == null)
            slipperyMaterial = new PhysicsMaterial2D("EnemySlippery") { friction = 0f, bounciness = 0f };
        return slipperyMaterial;
    }

    // Windup first: the damage lands attackHitDelay later, on the swing's impact frame.
    private void StartAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        swingTarget = target;
        pendingHitTime = Time.time + attackHitDelay;

        if (IsSpawned)
            swingCount.Value = (byte)(swingCount.Value + 1);
        PlayAttackAnimation();
    }

    // The swing only connects if its victim is still alive and roughly within reach when the impact lands.
    private void ResolveHit()
    {
        pendingHitTime = 0f;
        if (swingTarget == null || swingTarget.IsDead)
            return;

        float reach = attackRange * hitRangeTolerance;
        if (((Vector2)swingTarget.transform.position - rb.position).sqrMagnitude <= reach * reach)
            swingTarget.RequestDamage(damage);
    }

    private void PlayAttackAnimation()
    {
        if (animator == null)
            return;

        attackAnimationEndTime = Time.time + attackAnimationDuration;
        currentAnimationHash = AttackHash;
        animator.Play(AttackHash, 0, 0f);
    }

    // Faces the way it actually moved, and picks Attack/Run/Idle; only calls Play when the state changes.
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
