using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;

// Removes ghost offline players auto-spawned after each scene load.
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
