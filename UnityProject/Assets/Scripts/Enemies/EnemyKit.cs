using UnityEngine;

// Per-enemy-type stats/combat tuning, so new enemy variants are pure data, not code.
[CreateAssetMenu(fileName = "EnemyKit", menuName = "Transcendence/Enemy Kit")]
public class EnemyKit : ScriptableObject
{
    [Header("Classification")]
    [SerializeField] private int tier = 1;

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

    // Frames/frame rate live on the projectile prefab itself (see ArrowProjectile for the player-side equivalent), not here.
    [Header("Ranged Attack")]
    [SerializeField] private bool isRanged;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileMaxDistance = 8f;
    [SerializeField] private float projectileArcHeight = 0.8f;

    [Header("Targeting")]
    [SerializeField] private float retargetInterval = 0.25f;
    [SerializeField] private float steerInterval = 0.1f;
    [SerializeField] private float directChaseRange = 8f;
    [SerializeField] private float lineOfSightInterval = 0.3f;
    [SerializeField] private float maxDistanceFromPlayers = 40f;
    [SerializeField] private float leashCheckInterval = 2f;

    // Each enemy type is its own prefab variant (sprite/Animator baked in) of the shared base Enemy prefab, rather than one prefab re-skinned at spawn time.
    [Header("Visual")]
    [SerializeField] private GameObject enemyPrefab;
    // Aseprite-imported controllers name states after the source clip tags (e.g. "Shoot", "Walk"), which don't all match - kept configurable instead of renaming the generated asset.
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string runStateName = "Run";
    [SerializeField] private string attackStateName = "Attack";

    public int Tier => tier;
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
    public bool IsRanged => isRanged;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileMaxDistance => projectileMaxDistance;
    public float ProjectileArcHeight => projectileArcHeight;
    public float RetargetInterval => retargetInterval;
    public float SteerInterval => steerInterval;
    public float DirectChaseRange => directChaseRange;
    public float LineOfSightInterval => lineOfSightInterval;
    public float MaxDistanceFromPlayers => maxDistanceFromPlayers;
    public float LeashCheckInterval => leashCheckInterval;
    public GameObject EnemyPrefab => enemyPrefab;
    public string IdleStateName => idleStateName;
    public string RunStateName => runStateName;
    public string AttackStateName => attackStateName;
}
