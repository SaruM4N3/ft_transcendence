using Unity.Netcode;
using UnityEngine;

// Attack windup/hit-resolution/telegraph; EnemyAI decides when to attack and calls StartAttack.
public class EnemyActions : NetworkBehaviour
{
    private EnemyKit kit;

    private EnemyAI ai;
    private PlayerStats swingTarget;
    private float pendingHitTime;

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

    // Damage lands attackHitDelay later; swing state is always written (not gated on IsSpawned) so offline solo play works too.
    public void StartAttack(PlayerStats target)
    {
        swingTarget = target;
        pendingHitTime = Time.time + kit.AttackHitDelay;

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
        AttackTelegraph.Show(transform, ai.BodyCenter, direction, kit.AttackAngle, kit.AttackRange * kit.HitRangeTolerance, kit.AttackHitDelay);
    }
}
