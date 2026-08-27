using System;

// Common read side for anything WorldHealthBar (or similar UI) can bind to - DummyStats, EnemyStats.
public interface IHealthStats
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    event Action<float, float> OnHealthChanged;
}
