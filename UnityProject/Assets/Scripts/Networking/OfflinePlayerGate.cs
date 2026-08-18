using Unity.Netcode;
using UnityEngine;

/// <summary>Put on a scene-placed offline player (the same pattern as the Lobby's own Player, minus
/// NetworkBootstrap - a game mode scene like Coop has no Host/Join button of its own, it's only ever
/// reached already-networked via a scene transition from the Lobby). Without this, the placed
/// NetworkObject would get auto-spawned as a phantom extra "player" the instant the scene loads under
/// an active session - see NetworkBootstrap.CaptureOfflinePlayerState for the same bug diagnosed in the
/// Lobby. Solo testing (opening this scene directly and hitting Play) is unaffected: there's no active
/// session then, so this gate does nothing and the placed player behaves exactly as before.</summary>
public class OfflinePlayerGate : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            gameObject.SetActive(false);
    }
}
