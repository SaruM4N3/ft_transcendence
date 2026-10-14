using Unity.Netcode;
using UnityEngine;

// Attack windup/hit-resolution/telegraph; EnemyAI decides when to attack and calls StartAttack.
public class EnemyActions : NetworkBehaviour
{
    private EnemyKit kit;

    private EnemyAI ai;
    private Player swingTarget;
    private float pendingHitTime;
    // Set by the ranged projectile's own hitbox if it hits a shield mid-flight; ResolveHit checks it instead of the position-based shield check melee uses, since a flying shot's range can exceed a shield's radius.
    private bool rangedShotBlocked;

    public bool IsWindingUp => pendingHitTime > 0f;

    // Assigned at spawn time by whoever instantiates this enemy (see Enemy.Initialize); never baked into the prefab.
    public EnemyKit Kit { get => kit; set => kit = value; }

    private readonly NetworkVariable<byte> swingCount = new NetworkVariable<byte>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Swing direction in degrees, replicated so remote clients can reproduce the same telegraph the server resolves against.
    private readonly NetworkVariable<float> swingAngleDegrees = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        ai = GetComponent<EnemyAI>();
    }

    public override void OnNetworkSpawn()
    {
        swingCount.OnValueChanged += HandleSwingCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        swingCount.OnValueChanged -= HandleSwingCountChanged;
    }

    void FixedUpdate()
    {
        if (!this.HasServerAuthority())
            return;

        if (pendingHitTime > 0f && Time.time >= pendingHitTime)
            ResolveHit();
    }

    // Replays the swing on remote clients.
    private void HandleSwingCountChanged(byte previous, byte current)
    {
        if (!IsServer)
            PlayAttackAnimation();
    }

    // Damage lands attackHitDelay later for melee; for ranged it lands exactly when a shot travelling at ProjectileSpeed would reach the target's current position, so the hit stays in sync with the actual flying sprite instead of an unrelated fixed delay.
    public void StartAttack(Player target)
    {
        swingTarget = target;
        rangedShotBlocked = false;

        float hitDelay = kit.AttackHitDelay;
        if (kit.IsRanged && kit.ProjectileSpeed > 0f)
            hitDelay = Vector2.Distance(ai.BodyCenter, EnemyAI.TargetCenter(target)) / kit.ProjectileSpeed;
        pendingHitTime = Time.time + hitDelay;

        Vector2 direction = (EnemyAI.TargetCenter(target) - ai.BodyCenter).normalized;
        swingAngleDegrees.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (IsSpawned)
            swingCount.Value = (byte)(swingCount.Value + 1);
        PlayAttackAnimation(direction);
    }

    // Checked against the enemy's current position (not a frozen snapshot), so it always agrees with the telegraph, which follows it too.
    private void ResolveHit()
    {
        pendingHitTime = 0f;
        if (swingTarget == null || swingTarget.IsDead)
            return;

        Vector2 toVictim = EnemyAI.TargetCenter(swingTarget) - ai.BodyCenter;
        float reach = kit.AttackRange * kit.HitRangeTolerance;
        if (toVictim.sqrMagnitude > reach * reach)
            return;

        Vector2 swingDirection = SwingDirection();
        if (Vector2.Angle(swingDirection, toVictim) > kit.AttackAngle * 0.5f)
            return;

        if (kit.IsRanged)
        {
            if (rangedShotBlocked)
                return;
        }
        else if (Player.TryAbsorbWithShield(ai.BodyCenter, EnemyAI.TargetCenter(swingTarget), kit.Damage))
        {
            return;
        }

        swingTarget.RequestDamage(kit.Damage);
    }

    private Vector2 SwingDirection()
    {
        float radians = swingAngleDegrees.Value * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    // Remote clients reach this via HandleSwingCountChanged, with no local windup state of their own to use.
    private void PlayAttackAnimation() => PlayAttackAnimation(SwingDirection());

    private void PlayAttackAnimation(Vector2 direction)
    {
        ai.PlayAttackVisual(kit.AttackAnimationDuration);

        if (kit.IsRanged)
        {
            RangedAttackVisual.Show(kit.ProjectilePrefab, ai.BodyCenter, direction, kit.ProjectileSpeed, kit.ProjectileMaxDistance, kit.ProjectileArcHeight, kit.Damage, this.HasServerAuthority(), () => rangedShotBlocked = true);
            return;
        }

        AttackTelegraph.Show(transform, ai.BodyCenter, direction, kit.AttackAngle, kit.AttackRange * kit.HitRangeTolerance, kit.AttackHitDelay);
    }
}
