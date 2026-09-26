using UnityEngine;

// Place in a scene so players can take damage but never drop below 1 HP while it is loaded (e.g. the Lobby).
public class DeathProtectionZone : MonoBehaviour
{
    void OnEnable()
    {
        PlayerStats.DeathProtected = true;
    }

    void OnDisable()
    {
        PlayerStats.DeathProtected = false;
    }
}
