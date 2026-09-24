using UnityEngine;

// Shared setup for anything PlayerMovement spawns as an attack visual/hitbox.
public interface IAttackFX
{
    void Init(GameObject attacker, bool hasHitbox);
}
