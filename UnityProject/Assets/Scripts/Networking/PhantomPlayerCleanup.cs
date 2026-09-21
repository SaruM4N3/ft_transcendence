using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;

// Server-side safety net: a scene's own placed offline-player object can still get auto-spawned as a
// phantom despite OfflinePlayerGate's Awake-time deactivation - Netcode's own scene-population timing
// during a networked scene load can race ahead of that check. Sweep any such ghost away right after
// every scene finishes loading, for every scene (this object persists across all of them).
public class PhantomPlayerCleanup : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsServer && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadEventCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadEventCompleted;
    }

    private void HandleSceneLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsServer)
            StartCoroutine(CleanupPhantomsNextFrame());
    }

    private System.Collections.IEnumerator CleanupPhantomsNextFrame()
    {
        yield return null;

        List<PlayerCustomization> players = PlayerCustomization.AllActiveInstances.ToList();
        foreach (PlayerCustomization player in players)
        {
            NetworkObject networkObject = player.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned && !networkObject.IsPlayerObject)
            {
                networkObject.Despawn(false);
                networkObject.gameObject.SetActive(false);
            }
        }
    }
}
