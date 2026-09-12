// Common damage entry point for anything a SlashAttackFX (or future attack) can hit.
public interface IDamageable
{
    void RequestDamage(float amount);
}
