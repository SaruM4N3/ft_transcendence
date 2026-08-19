using Unity.Netcode;
using UnityEngine;

// Resolves the local client's own player: the spawned networked instance once a session is running,
// or the offline solo player (tag lookup) before Host/Join or when there's no session at all.
public static class LocalPlayer
{
    public static GameObject Get()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
            return nm.LocalClient.PlayerObject.gameObject;

        return GameObject.FindWithTag("Player");
    }

    public static PlayerCustomization GetCustomization()
    {
        GameObject player = Get();
        return player != null ? player.GetComponent<PlayerCustomization>() : null;
    }
}
