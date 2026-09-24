using UnityEngine;

// Per-class combat timing/FX a class is defined by.
[CreateAssetMenu(fileName = "ClassKit", menuName = "Transcendence/Class Kit")]
public class ClassKit : ScriptableObject
{
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float specialCooldown = 2f;
    [SerializeField] private float ultimateCooldown = 5f;
    [SerializeField] private GameObject attackFxPrefab;
    [SerializeField] private float attackFxDistance = 1f;
    [SerializeField] private float attackMoveLockDuration = 0.3f;
    [SerializeField] private float attackMoveSpeedMultiplier = 0.35f;

    public float AttackCooldown => attackCooldown;
    public float SpecialCooldown => specialCooldown;
    public float UltimateCooldown => ultimateCooldown;
    public GameObject AttackFxPrefab => attackFxPrefab;
    public float AttackFxDistance => attackFxDistance;
    public float AttackMoveLockDuration => attackMoveLockDuration;
    public float AttackMoveSpeedMultiplier => attackMoveSpeedMultiplier;
}
