using System;

// Read-only health view that WorldHealthBar binds to.
public interface IHealthStats
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    event Action<float, float> OnHealthChanged;
}
