using Unity.Netcode;
using UnityEngine;

// Deactivates a scene-placed offline player once a session is active, so it doesn't auto-spawn as a phantom.
// Same bug NetworkBootstrap.CaptureOfflinePlayerState fixes in the Lobby; no-op outside a session.
public class OfflinePlayerGate : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            gameObject.SetActive(false);
    }
}
