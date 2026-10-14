using UnityEngine;
using UnityEngine.Serialization;

// Per-class combat timing/FX; fields are grouped per-ability so each of Attack/Special/Ultimate is self-contained.
[CreateAssetMenu(fileName = "ClassKit", menuName = "Transcendence/Class Kit")]
public class ClassKit : ScriptableObject
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthRegenPerSecond = 1f;

    [Header("Mana")]
    [SerializeField] private float maxMana = 50f;
    [SerializeField] private float manaRegenPerSecond = 1f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private GameObject attackFxPrefab;
    [SerializeField] private float attackFxDistance = 1f;
    [FormerlySerializedAs("damage")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackMoveLockDuration = 0.3f;
    [SerializeField] private float attackMoveSpeedMultiplier = 0.35f;
    [SerializeField] private float attackAnimSpeed = 1f;
    [SerializeField] private float attackSize = 1f;
    [SerializeField] private bool attackIgnoreAimRotation;
    [SerializeField] private bool attackFollowCaster = true;

    [Header("Attack Projectile")]
    [SerializeField] private bool hasProjectile;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileMaxDistance = 10f;
    [SerializeField] private int pierceCount = 1;

    [Header("Special")]
    [SerializeField] private float specialCooldown = 2f;
    [SerializeField] private float specialManaCost;
    [SerializeField] private GameObject specialFxPrefab;
    [SerializeField] private float specialFxDistance = 1f;
    [SerializeField] private float specialDamage = 15f;
    [SerializeField] private float specialMoveLockDuration = 0.3f;
    [SerializeField] private float specialMoveSpeedMultiplier = 0.35f;
    [SerializeField] private float specialAnimSpeed = 1f;
    [SerializeField] private float specialSize = 1f;
    [SerializeField] private bool specialIgnoreAimRotation;
    [SerializeField] private bool specialFollowCaster = true;

    [Header("Ultimate")]
    [SerializeField] private float ultimateCooldown = 5f;
    [SerializeField] private float ultimateManaCost;
    [SerializeField] private GameObject ultimateFxPrefab;
    [SerializeField] private float ultimateFxDistance = 1f;
    [SerializeField] private float ultimateDamage = 15f;
    [SerializeField] private float ultimateMoveLockDuration = 0.3f;
    [SerializeField] private float ultimateMoveSpeedMultiplier = 0.35f;
    [SerializeField] private float ultimateAnimSpeed = 1f;
    [SerializeField] private float ultimateSize = 1f;
    [SerializeField] private bool ultimateIgnoreAimRotation;
    [SerializeField] private bool ultimateFollowCaster = true;

    [Header("Ultimate Shield")]
    [SerializeField] private bool hasShield;
    [SerializeField] private float shieldHoldDuration = 10f;
    [SerializeField] private float shieldHealth = 150f;
    [SerializeField] private float shieldKnockbackForce = 8f;
    [SerializeField] private float shieldKnockbackDuration = 0.25f;
    [SerializeField] private float shieldPushInterval = 0.5f;
    [SerializeField] private float shieldTurnSpeed = 120f;
    // When true, the character's Ultimate cast animation loops/holds instead of auto-returning to Idle/Walk, for as long as the shield is up - needs a "ShieldHeld" bool param on that class's Animator Controller to have any effect.
    [SerializeField] private bool shieldHoldAnimation;

    public float MaxHealth => maxHealth;
    public float HealthRegenPerSecond => healthRegenPerSecond;
    public float MaxMana => maxMana;
    public float ManaRegenPerSecond => manaRegenPerSecond;

    public float AttackCooldown => attackCooldown;
    public GameObject AttackFxPrefab => attackFxPrefab;
    public float AttackFxDistance => attackFxDistance;
    public float AttackDamage => attackDamage;
    public float AttackMoveLockDuration => attackMoveLockDuration;
    public float AttackMoveSpeedMultiplier => attackMoveSpeedMultiplier;
    public float AttackAnimSpeed => attackAnimSpeed;
    public float AttackSize => attackSize;
    public bool AttackIgnoreAimRotation => attackIgnoreAimRotation;
    public bool AttackFollowCaster => attackFollowCaster;

    public bool HasProjectile => hasProjectile;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileMaxDistance => projectileMaxDistance;
    public int PierceCount => pierceCount;

    public float SpecialCooldown => specialCooldown;
    public float SpecialManaCost => specialManaCost;
    public GameObject SpecialFxPrefab => specialFxPrefab;
    public float SpecialFxDistance => specialFxDistance;
    public float SpecialDamage => specialDamage;
    public float SpecialMoveLockDuration => specialMoveLockDuration;
    public float SpecialMoveSpeedMultiplier => specialMoveSpeedMultiplier;
    public float SpecialAnimSpeed => specialAnimSpeed;
    public float SpecialSize => specialSize;
    public bool SpecialIgnoreAimRotation => specialIgnoreAimRotation;
    public bool SpecialFollowCaster => specialFollowCaster;

    public float UltimateCooldown => ultimateCooldown;
    public float UltimateManaCost => ultimateManaCost;
    public GameObject UltimateFxPrefab => ultimateFxPrefab;
    public float UltimateFxDistance => ultimateFxDistance;
    public float UltimateDamage => ultimateDamage;
    public float UltimateMoveLockDuration => ultimateMoveLockDuration;
    public float UltimateMoveSpeedMultiplier => ultimateMoveSpeedMultiplier;
    public float UltimateAnimSpeed => ultimateAnimSpeed;
    public float UltimateSize => ultimateSize;
    public bool UltimateIgnoreAimRotation => ultimateIgnoreAimRotation;
    public bool UltimateFollowCaster => ultimateFollowCaster;

    public bool HasShield => hasShield;
    public float ShieldHoldDuration => shieldHoldDuration;
    public float ShieldHealth => shieldHealth;
    public float ShieldKnockbackForce => shieldKnockbackForce;
    public float ShieldKnockbackDuration => shieldKnockbackDuration;
    public float ShieldPushInterval => shieldPushInterval;
    public float ShieldTurnSpeed => shieldTurnSpeed;
    public bool ShieldHoldAnimation => shieldHoldAnimation;
}
