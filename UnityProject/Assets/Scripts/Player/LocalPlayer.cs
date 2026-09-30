using Unity.Netcode;
using UnityEngine;

// Resolves the local player: the spawned instance in a session, else the offline one.
public static class LocalPlayer
{
    public static GameObject Get()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
            return nm.LocalClient.PlayerObject.gameObject;

        return GameObject.FindWithTag("Player");
    }

    public static Player GetPlayer()
    {
        GameObject player = Get();
        return player != null ? player.GetComponent<Player>() : null;
    }
}
