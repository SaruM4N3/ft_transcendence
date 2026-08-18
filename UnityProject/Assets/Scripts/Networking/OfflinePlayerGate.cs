using Unity.Netcode;
using UnityEngine;

/// <summary>Put on a scene-placed offline player in any scene reached only via a networked scene
/// transition (no Host/Join button of its own, e.g. a game mode scene). Without this, its NetworkObject
/// gets auto-spawned as a phantom extra "player" the instant the scene loads mid-session - see
/// NetworkBootstrap.CaptureOfflinePlayerState for the same bug in the Lobby. Solo testing (opening the
/// scene directly and hitting Play) is unaffected - no active session, so this does nothing.</summary>
public class OfflinePlayerGate : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            gameObject.SetActive(false);
    }
}
