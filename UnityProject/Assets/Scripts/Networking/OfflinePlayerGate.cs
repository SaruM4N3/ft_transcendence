using Unity.Netcode;
using UnityEngine;

// Put on a scene-placed offline player in scenes only reached via a networked scene transition (no
// Host/Join of its own). Without this its NetworkObject auto-spawns as a phantom player mid-session -
// same bug as NetworkBootstrap.CaptureOfflinePlayerState in the Lobby. No-op outside an active session.
public class OfflinePlayerGate : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            gameObject.SetActive(false);
    }
}
