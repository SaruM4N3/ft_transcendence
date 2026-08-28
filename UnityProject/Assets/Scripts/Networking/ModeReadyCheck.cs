using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Networked mode-select ready check: any client can propose a mode (GameModeLoader), broadcast to
// everyone, then load the scene together via NetworkSceneManager once all players are ready.
public class ModeReadyCheck : NetworkBehaviour
{
    // Same inactive-inclusive fallback as CharacterCustomizationMenu.Instance.
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

    // Empty = no ready check in progress. Set only via the ServerRpc below so readiness resets atomically.
    private readonly NetworkVariable<FixedString64Bytes> pendingSceneName = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event System.Action<string> OnPendingSceneChanged;

    private void Awake()
    {
        instance = this;
        pendingSceneName.OnValueChanged += (_, newValue) => OnPendingSceneChanged?.Invoke(newValue.ToString());
    }

    public void RequestReadyCheck(string sceneName)
    {
        RequestReadyCheckServerRpc(sceneName);
    }

    // RequireOwnership = false: this NetworkObject is server-owned, but any client should be able to
    // propose a mode, not just the host.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
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
