using System;
using UnityEngine;

// A practice target (e.g. the Lobby's training dummy) - not networked, unlike PlayerStats.
public class DummyStats : MonoBehaviour, IDamageable, IHealthStats
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField] private float regenDelay = 3f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public event Action<float, float> OnHealthChanged;

    private float lastHitTime = -999f;

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    void Update()
    {
        if (CurrentHealth >= maxHealth || Time.time < lastHitTime + regenDelay)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + regenPerSecond * Time.deltaTime);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void RequestDamage(float amount)
    {
        lastHitTime = Time.time;
        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
