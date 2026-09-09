using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxMana = 50f;

    public float MaxHealth => maxHealth;
    public float MaxMana => maxMana;
    public float CurrentHealth { get; private set; }
    public float CurrentMana { get; private set; }

    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;

    void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentMana = maxMana;
    }

    void Start()
    {
        // Every connected player's stats live on their own machine too (one instance per client),
        // but only the local/owned instance should drive this client's HUD.
        if (!this.IsLocallyControlled())
            return;

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnManaChanged?.Invoke(CurrentMana, maxMana);
    }

    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, maxHealth);
        if (this.IsLocallyControlled())
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
        if (this.IsLocallyControlled())
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
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
