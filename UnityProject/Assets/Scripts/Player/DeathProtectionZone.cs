using UnityEngine;

// Keeps players at 1 HP minimum while loaded, e.g. in the Lobby.
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
