using UnityEngine;

// Per-class combat timing/FX a class is defined by.
[CreateAssetMenu(fileName = "ClassKit", menuName = "Transcendence/Class Kit")]
public class ClassKit : ScriptableObject
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthRegenPerSecond = 1f;

    [Header("Mana")]
    [SerializeField] private float maxMana = 50f;
    [SerializeField] private float manaRegenPerSecond = 1f;

    [Header("Cooldowns")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float specialCooldown = 2f;
    [SerializeField] private float ultimateCooldown = 5f;

    [Header("Attack FX")]
    [SerializeField] private GameObject attackFxPrefab;
    [SerializeField] private float attackFxDistance = 1f;
    [SerializeField] private float damage = 15f;

    [Header("Projectile")]
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileMaxDistance = 10f;
    [SerializeField] private int pierceCount = 1;

    [Header("Movement")]
    [SerializeField] private float attackMoveLockDuration = 0.3f;
    [SerializeField] private float attackMoveSpeedMultiplier = 0.35f;

    [Header("Animation Speed")]
    [SerializeField] private float attackAnimSpeed = 1f;
    [SerializeField] private float specialAnimSpeed = 1f;

    public float MaxHealth => maxHealth;
    public float HealthRegenPerSecond => healthRegenPerSecond;
    public float MaxMana => maxMana;
    public float ManaRegenPerSecond => manaRegenPerSecond;
    public float AttackCooldown => attackCooldown;
    public float SpecialCooldown => specialCooldown;
    public float UltimateCooldown => ultimateCooldown;
    public GameObject AttackFxPrefab => attackFxPrefab;
    public float AttackFxDistance => attackFxDistance;
    public float Damage => damage;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileMaxDistance => projectileMaxDistance;
    public int PierceCount => pierceCount;
    public float AttackMoveLockDuration => attackMoveLockDuration;
    public float AttackMoveSpeedMultiplier => attackMoveSpeedMultiplier;
    public float AttackAnimSpeed => attackAnimSpeed;
    public float SpecialAnimSpeed => specialAnimSpeed;
}
