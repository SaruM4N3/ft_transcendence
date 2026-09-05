using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

// Game-feel for the player: hurt (flash, screen shake, red edges) and attack (swing kick, hit-confirm shake).
[RequireComponent(typeof(PlayerStats))]
public class PlayerFeedback : NetworkBehaviour
{
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private SpriteHitFlash hitFlash;

    [SerializeField] private float hurtShake = 0.35f;
    [SerializeField] private float hurtVignetteStrength = 0.6f;
    [SerializeField] private float hurtVignetteDuration = 0.45f;
    [SerializeField] private float lightAttackShake = 0.06f;
    [SerializeField] private float heavyAttackShake = 0.15f;
    [SerializeField] private float hitLandedShake = 0.12f;

    private PlayerStats stats;
    private ScreenEdgeFlash edgeFlash;
    private float lastHealth = -1f;

    // Skips the customization preview copy, which has no NetworkObject and would break the static ability event.
    void Awake()
    {
        if (GetComponent<NetworkObject>() == null)
        {
            enabled = false;
            return;
        }

        stats = GetComponent<PlayerStats>();
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
        if (hitFlash == null)
            hitFlash = GetComponent<SpriteHitFlash>();
    }

    void OnEnable()
    {
        stats.OnHealthReplicated += HandleHealthReplicated;
        PlayerMovement.OnAbilityUsed += HandleAbilityUsed;
        SlashAttackFX.OnHitLanded += HandleHitLanded;
    }

    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthReplicated -= HandleHealthReplicated;
        PlayerMovement.OnAbilityUsed -= HandleAbilityUsed;
        SlashAttackFX.OnHitLanded -= HandleHitLanded;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (edgeFlash != null)
            Destroy(edgeFlash.gameObject);
    }

    // Fires for every observer, so everyone sees the flash but only the owner gets shake and red edges.
    private void HandleHealthReplicated(float current, float max)
    {
        bool tookDamage = lastHealth >= 0f && current < lastHealth;
        lastHealth = current;
        if (!tookDamage)
            return;

        hitFlash?.Flash();
        if (!this.IsLocallyControlled())
            return;

        Shake(hurtShake);
        if (edgeFlash == null)
            edgeFlash = ScreenEdgeFlash.Create();
        edgeFlash.Pulse(hurtVignetteStrength, hurtVignetteDuration);
    }

    private void HandleAbilityUsed(AbilityType ability, float cooldown)
    {
        if (!this.IsLocallyControlled())
            return;

        if (ability == AbilityType.LightAttack)
            Shake(lightAttackShake);
        else if (ability == AbilityType.HeavyAttack)
            Shake(heavyAttackShake);
    }

    // Only the attacker's own hitbox raises this, so it never fires for a remote player's attack.
    private void HandleHitLanded(GameObject attacker, Vector3 position)
    {
        if (attacker == gameObject && this.IsLocallyControlled())
            Shake(hitLandedShake);
    }

    private void Shake(float force)
    {
        if (impulseSource != null)
            impulseSource.GenerateImpulse(Random.insideUnitCircle.normalized * force);
    }
}
