using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Networked mode-select ready check: any client can propose a game mode (see
/// GameModeLoader), which broadcasts a "ready check" to every connected client's screen. The scene
/// loads - via Netcode's own NetworkSceneManager, so every client transitions together - once every
/// currently connected player has marked themselves ready (PlayerCustomization.IsReady).</summary>
public class ModeReadyCheck : NetworkBehaviour
{
    /// <summary>Same inactive-inclusive fallback reasoning as CharacterCustomizationMenu.Instance -
    /// this lives on the always-active NetworkManager GameObject, but callers shouldn't have to care
    /// about Awake ordering.</summary>
    public static ModeReadyCheck Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<ModeReadyCheck>(FindObjectsInactive.Include);
            return instance;
        }
    }
    private static ModeReadyCheck instance;

    // Empty = no ready check in progress. Server-writable only - clients propose a mode through the
    // ServerRpc below rather than writing this directly, so the server can reset everyone's readiness
    // at the same moment a new check starts.
    private readonly NetworkVariable<FixedString64Bytes> pendingSceneName = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Fires with the pending scene name (empty string when the check ends) - the ready check
    /// UI opens/closes itself off this.</summary>
    public event System.Action<string> OnPendingSceneChanged;

    private void Awake()
    {
        instance = this;
        pendingSceneName.OnValueChanged += (_, newValue) => OnPendingSceneChanged?.Invoke(newValue.ToString());
    }

    /// <summary>Called by GameModeLoader when a mode button is pressed during a multiplayer session.</summary>
    public void RequestReadyCheck(string sceneName)
    {
        RequestReadyCheckServerRpc(sceneName);
    }

    // RequireOwnership = false: this NetworkObject (the NetworkManager) is owned by the server, but any
    // connected client needs to be able to propose a mode, not just the host.
    [ServerRpc(RequireOwnership = false)]
    private void RequestReadyCheckServerRpc(FixedString64Bytes sceneName)
    {
        pendingSceneName.Value = sceneName;
        foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
            player.ServerResetReady();
    }

    // Polls rather than wiring a change-event per currently-connected player (which would need
    // subscribe/unsubscribe on every join/leave while a check is in progress) - this only does
    // meaningful work while IsServer and a check is actually pending, which is a short-lived,
    // low-frequency state, so the per-frame cost is negligible.
    private void Update()
    {
        if (!IsServer || pendingSceneName.Value.IsEmpty)
            return;

        if (PlayerCustomization.AllActiveInstances.Count == 0)
            return;

        foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
            if (!player.IsReady)
                return;

        string sceneName = pendingSceneName.Value.ToString();
        pendingSceneName.Value = default;
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
