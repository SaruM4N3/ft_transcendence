using UnityEngine;

// Per-enemy-type stats/combat tuning, so new enemy variants are pure data, not code.
[CreateAssetMenu(fileName = "EnemyKit", menuName = "Transcendence/Enemy Kit")]
public class EnemyKit : ScriptableObject
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 40f;

    [Header("Rewards")]
    [SerializeField] private int xpReward = 10;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float attackAngle = 100f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackHitDelay = 0.3f;
    [SerializeField] private float attackAnimationDuration = 0.7f;
    [SerializeField] private float attackMoveSpeedMultiplier = 0.25f;
    [SerializeField] private float hitRangeTolerance = 1.5f;

    [Header("Targeting")]
    [SerializeField] private float retargetInterval = 0.25f;
    [SerializeField] private float steerInterval = 0.1f;
    [SerializeField] private float directChaseRange = 8f;
    [SerializeField] private float lineOfSightInterval = 0.3f;
    [SerializeField] private float maxDistanceFromPlayers = 40f;
    [SerializeField] private float leashCheckInterval = 2f;

    [Header("Visual")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private RuntimeAnimatorController animatorController;

    public float MaxHealth => maxHealth;
    public int XpReward => xpReward;
    public float MoveSpeed => moveSpeed;
    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;
    public float Damage => damage;
    public float AttackCooldown => attackCooldown;
    public float AttackHitDelay => attackHitDelay;
    public float AttackAnimationDuration => attackAnimationDuration;
    public float AttackMoveSpeedMultiplier => attackMoveSpeedMultiplier;
    public float HitRangeTolerance => hitRangeTolerance;
    public float RetargetInterval => retargetInterval;
    public float SteerInterval => steerInterval;
    public float DirectChaseRange => directChaseRange;
    public float LineOfSightInterval => lineOfSightInterval;
    public float MaxDistanceFromPlayers => maxDistanceFromPlayers;
    public float LeashCheckInterval => leashCheckInterval;
    public Sprite Sprite => sprite;
    public RuntimeAnimatorController AnimatorController => animatorController;
}
