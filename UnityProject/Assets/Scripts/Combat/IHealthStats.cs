using System;

// Read-only health view generic feedback (health bars, hit flash, damage numbers) binds to.
public interface IHealthStats
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    event Action<float, float> OnHealthChanged;
}
