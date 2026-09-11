using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxMana = 50f;

    // Replicated so the lobby roster can show every player's health bar, not just the local one - Mana
    // stays purely local since nothing outside this client needs to see it.
    private readonly NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float MaxHealth => maxHealth;
    public float MaxMana => maxMana;
    public float CurrentHealth => currentHealth.Value;
    public float CurrentMana { get; private set; }

    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;

    /// <summary>Fires for THIS instance specifically (any player, not just the local one) - the lobby
    /// roster binds to a specific player's health through this instead of the static OnHealthChanged,
    /// which only ever reflects the local player.</summary>
    public event Action<float, float> OnHealthReplicated;

    void Awake()
    {
        CurrentMana = maxMana;
        currentHealth.OnValueChanged += (_, newValue) =>
        {
            OnHealthReplicated?.Invoke(newValue, maxHealth);
            if (this.IsLocallyControlled())
                OnHealthChanged?.Invoke(newValue, maxHealth);
        };
        currentHealth.Value = maxHealth;
    }

    void Start()
    {
        // Only the local/owned instance should drive this client's HUD.
        if (!this.IsLocallyControlled())
            return;

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnManaChanged?.Invoke(CurrentMana, maxMana);
    }

    // currentHealth is Owner-writable, so a non-owner call would be silently dropped by Netcode anyway
    // - this just avoids the warning it logs for that.
    public void TakeDamage(float amount)
    {
        if (!this.IsLocallyControlled())
            return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0f, maxHealth);
    }

    public void Heal(float amount)
    {
        if (!this.IsLocallyControlled())
            return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0f, maxHealth);
    }

    public bool TrySpendMana(float amount)
    {
        if (CurrentMana < amount)
            return false;

        CurrentMana -= amount;
        if (this.IsLocallyControlled())
            OnManaChanged?.Invoke(CurrentMana, maxMana);
        return true;
    }

    public void RestoreMana(float amount)
    {
        CurrentMana = Mathf.Clamp(CurrentMana + amount, 0f, maxMana);
        if (this.IsLocallyControlled())
            OnManaChanged?.Invoke(CurrentMana, maxMana);
    }
}
