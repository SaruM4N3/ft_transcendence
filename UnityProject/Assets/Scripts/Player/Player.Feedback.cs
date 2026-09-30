using Unity.Cinemachine;
using UnityEngine;

public partial class Player
{
    [Header("References")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private SpriteHitFlash hitFlash;

    [Header("Feedback")]
    [SerializeField] private float hurtShake = 0.35f;
    [SerializeField] private float hurtVignetteStrength = 0.6f;
    [SerializeField] private float hurtVignetteDuration = 0.45f;
    [SerializeField] private float attackShake = 0.06f;
    [SerializeField] private float specialShake = 0.15f;
    [SerializeField] private float hitLandedShake = 0.12f;

    private ScreenEdgeFlash edgeFlash;
    private float lastHealth = -1f;

    private void AwakeFeedback()
    {
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
        if (hitFlash == null)
            hitFlash = GetComponent<SpriteHitFlash>();
    }

    private void OnEnableFeedback()
    {
        OnHealthReplicated += HandleHealthReplicated;
        OnAbilityUsed += HandleAbilityUsed;
        SlashAttackFX.OnHitLanded += HandleHitLanded;
    }

    private void OnDisableFeedback()
    {
        OnHealthReplicated -= HandleHealthReplicated;
        OnAbilityUsed -= HandleAbilityUsed;
        SlashAttackFX.OnHitLanded -= HandleHitLanded;
    }

    private void OnDestroyFeedback()
    {
        if (edgeFlash != null)
            Destroy(edgeFlash.gameObject);
    }

    // Everyone sees the flash; only the owner gets shake and red edges.
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

        if (ability == AbilityType.Attack)
            Shake(attackShake);
        else if (ability == AbilityType.Special)
            Shake(specialShake);
    }

    // Raised only by the attacker's own hitbox.
    private void HandleHitLanded(GameObject attacker, Vector3 position)
    {
        if (attacker == gameObject && this.IsLocallyControlled())
            Shake(hitLandedShake);
    }

    private void Shake(float force)
    {
        if (impulseSource != null && GameSettings.ScreenShakeEnabled)
            impulseSource.GenerateImpulse(Random.insideUnitCircle.normalized * force);
    }
}
