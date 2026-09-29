using UnityEngine;

// Shared setup for anything Player spawns as an attack visual/hitbox.
public interface IAttackFX
{
    void Init(GameObject attacker, bool hasHitbox, float damage);
}
