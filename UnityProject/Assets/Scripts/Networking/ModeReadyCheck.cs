using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Networked mode-select ready check; loads the scene once everyone is ready.
public class ModeReadyCheck : NetworkBehaviour
{
    // Inactive-inclusive fallback lookup.
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

    // Empty means no check in progress; set only via the ServerRpc.
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

    // Any client can propose a mode, not just the host.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestReadyCheckServerRpc(FixedString64Bytes sceneName)
    {
        pendingSceneName.Value = sceneName;
        ResetAllReady();
    }

    // Player objects persist across scenes, so readiness is cleared per check.
    private static void ResetAllReady()
    {
        foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
            player.ServerResetReady();
    }

    // Polls instead of subscribing per player; cheap unless a check is pending.
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
        ResetAllReady();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
