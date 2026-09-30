using Unity.Netcode.Components;
using UnityEngine;

// Central reference hub for an enemy's parts, so other scripts can grab everything through one component.
public class Enemy : MonoBehaviour
{
    [Header("AI")]
    [SerializeField] private EnemyAI ai;

    [Header("Actions")]
    [SerializeField] private EnemyActions actions;

    [Header("Stats")]
    [SerializeField] private EnemyStats stats;

    [Header("Visuals")]
    [SerializeField] private SpriteHitFlash hitFlash;
    [SerializeField] private DamageNumberSpawner damageNumbers;

    [Header("Networking")]
    [SerializeField] private NetworkTransform networkTransform;

    public EnemyAI AI => ai;
    public EnemyActions Actions => actions;
    public EnemyStats Stats => stats;
    public SpriteHitFlash HitFlash => hitFlash;
    public DamageNumberSpawner DamageNumbers => damageNumbers;
    public NetworkTransform NetworkTransform => networkTransform;

    // Assigns the per-type kit and applies its visual; called by whoever spawns this enemy (see WaveSpawner).
    public void Initialize(EnemyKit kit)
    {
        ai.Kit = kit;
        actions.Kit = kit;
        stats.Kit = kit;
        ai.ApplyVisual(kit.Sprite, kit.AnimatorController);
    }

    private void Reset()
    {
        ai = GetComponent<EnemyAI>();
        actions = GetComponent<EnemyActions>();
        stats = GetComponent<EnemyStats>();
        hitFlash = GetComponent<SpriteHitFlash>();
        damageNumbers = GetComponent<DamageNumberSpawner>();
        networkTransform = GetComponent<NetworkTransform>();
    }
}
