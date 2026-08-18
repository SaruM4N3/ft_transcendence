using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Networked mode-select ready check: any client can propose a mode (see GameModeLoader),
/// broadcasting it to every connected client's screen. Loads the scene via NetworkSceneManager - so
/// everyone transitions together - once all players are marked ready (PlayerCustomization.IsReady).</summary>
public class ModeReadyCheck : NetworkBehaviour
{
    /// <summary>Same inactive-inclusive fallback as CharacterCustomizationMenu.Instance, so callers
    /// don't have to care about Awake ordering.</summary>
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

    // Empty = no ready check in progress. Clients propose a mode via the ServerRpc below (not a direct
    // write) so the server can reset everyone's readiness at the same moment a new check starts.
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

    // RequireOwnership = false: this NetworkObject is server-owned, but any client should be able to
    // propose a mode, not just the host.
    [ServerRpc(RequireOwnership = false)]
    private void RequestReadyCheckServerRpc(FixedString64Bytes sceneName)
    {
        pendingSceneName.Value = sceneName;
        foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
            player.ServerResetReady();
    }

    // Polls instead of wiring a per-player change-event (avoids subscribe/unsubscribe on every
    // join/leave); only does real work while a check is pending, so the per-frame cost is negligible.
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
