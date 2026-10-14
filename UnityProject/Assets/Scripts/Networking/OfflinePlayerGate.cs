using Unity.Netcode;
using UnityEngine;

// Deactivates the scene's offline player once a session is active.
public class OfflinePlayerGate : NetworkBehaviour
{
    private void Awake()
    {
        TryDeactivate();
    }

    private void Start()
    {
        TryDeactivate();
    }

    private void TryDeactivate()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkObject.IsPlayerObject)
            return;

        gameObject.SetActive(false);

        if (IsServer)
            NetworkObject.Despawn(true);
    }
}
