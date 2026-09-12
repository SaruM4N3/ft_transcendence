using System.Reflection;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

// Wires the Lobby's Host/Join buttons to Netcode over Unity Relay. The scene's offline player stays
// active for solo play - only Host/Join replaces it with a networked, Netcode-spawned one.
public class NetworkBootstrap : MonoBehaviour
{
#if UNITY_EDITOR
    // Suppresses Netcode's harmless "written before spawn" warning - the offline player writes early on purpose.
    // Editor-only: touching this type via reflection at startup crashes IL2CPP/WebGL builds ("indirect call to null").
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SuppressPreSpawnNetworkVariableWarning()
    {
        typeof(NetworkVariableBase)
            .GetField("IgnoreInitializeWarning", BindingFlags.NonPublic | BindingFlags.Static)
            ?.SetValue(null, true);
    }
#endif


    [SerializeField] private MenuPanel multiplayerPanel;

    // Doubles as the relay join-code field: hosting writes the code here (read-only) to copy; joining reads it.
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxPlayers = 4;

    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text joinStatusText;

    // Persists the relay code in the HUD after MultiplayerPanel closes.
    [SerializeField] private TMP_Text sessionCodeText;

    // Captured pre-Host so ApprovalCheck can spawn the host back where they stood, not at spawnPoint.
    private Vector3? hostSpawnPosition;
    private Quaternion hostSpawnRotation;

    // Holds the offline player's camera while reparented off during session setup - see CaptureOfflinePlayerState.
    private GameObject detachedCameraHolder;

    // NetworkManager.Singleton is only set in its own Awake(); Start() runs after every Awake().
    private void Start()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;

        // Host's own connection is always client id 0 in client-server topology.
        if (request.ClientNetworkId == NetworkManager.ServerClientId && hostSpawnPosition.HasValue)
        {
            response.Position = hostSpawnPosition.Value;
            response.Rotation = hostSpawnRotation;
        }
        else if (spawnPoint != null)
        {
            response.Position = spawnPoint.position;
            response.Rotation = spawnPoint.rotation;
        }
    }

    public async void HostGame()
    {
        // Relay code doesn't exist until CreateSessionAsync returns - show a placeholder while waiting.
        string originalPlaceholder = null;
        TMP_Text placeholderText = ipInputField != null ? ipInputField.placeholder as TMP_Text : null;
        if (placeholderText != null)
        {
            originalPlaceholder = placeholderText.text;
            placeholderText.text = "Generating code...";
        }
        if (sessionCodeText != null)
            sessionCodeText.text = string.Empty;

        try
        {
            await EnsureSignedInAsync();

            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState();
            hostSpawnPosition = customization.position;
            hostSpawnRotation = customization.rotation;

            // Allocates the relay, wires UnityTransport, and starts Netcode as host - no manual StartHost() needed.
            SessionOptions options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
            IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);

            if (ipInputField != null)
            {
                ipInputField.text = session.Code;
                ipInputField.interactable = false;
            }
            if (sessionCodeText != null)
                // Liberation Sans is more legible for a code players read/type back; "Code:" keeps the HUD font.
                sessionCodeText.text = $"Code: <font=\"LiberationSans SDF\">{session.Code}</font>";

            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex, customization.playerName));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to host - {e.Message}");
            if (placeholderText != null)
                placeholderText.text = originalPlaceholder;
        }
    }

    public async void JoinGame()
    {
        string code = ipInputField != null ? ipInputField.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(code))
        {
            SetJoinStatus("Enter a join code before joining.", isError: true);
            return;
        }

        SetJoinControlsInteractable(false);
        SetJoinStatus("Searching for the lobby...", isError: false);

        try
        {
            await EnsureSignedInAsync();

            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState();

            // Wires UnityTransport and starts Netcode as client - no manual StartClient() needed.
            await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            SetJoinStatus(string.Empty, isError: false);
            multiplayerPanel.Close();

            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex, customization.playerName));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to join - {e.Message}");
            SetJoinStatus("Couldn't find that lobby - check the code and try again.", isError: true);
        }
        finally
        {
            // Re-enable even on success so controls are usable next time the panel opens.
            SetJoinControlsInteractable(true);
        }
    }

    private void SetJoinControlsInteractable(bool interactable)
    {
        if (joinButton != null)
            joinButton.interactable = interactable;
        if (ipInputField != null)
            ipInputField.interactable = interactable;
    }

    private static readonly Color JoinErrorColor = new Color(0.85f, 0.2f, 0.2f);

    private void SetJoinStatus(string message, bool isError)
    {
        if (joinStatusText == null)
            return;

        joinStatusText.text = message;
        joinStatusText.color = isError ? JoinErrorColor : Color.white;
    }

    private static async System.Threading.Tasks.Task EnsureSignedInAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // Deactivates the offline player before Host/Join so it doesn't auto-spawn as a phantom; detaches its camera first.
    // Returns its class/color/name so the caller can copy them onto the replacement.
    private (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) CaptureOfflinePlayerState()
    {
        GameObject offlinePlayer = LocalPlayer.Get();
        if (offlinePlayer == null)
            return (0, 0, string.Empty, Vector3.zero, Quaternion.identity);

        PlayerCustomization customization = offlinePlayer.GetComponent<PlayerCustomization>();
        (int classIndex, int colorIndex, string playerName) current = customization != null
            ? (customization.ClassIndex, customization.ColorIndex, customization.PlayerName)
            : (0, 0, string.Empty);
        Vector3 position = offlinePlayer.transform.position;
        Quaternion rotation = offlinePlayer.transform.rotation;

        detachedCameraHolder = PlayerCameraRig.Detach(offlinePlayer.transform);

        offlinePlayer.SetActive(false);
        return (current.classIndex, current.colorIndex, current.playerName, position, rotation);
    }

    // Netcode's auto-spawned player starts at PlayerCustomization defaults - reapply what the offline player had.
    private System.Collections.IEnumerator RestoreLocalCustomizationWhenSpawned(int classIndex, int colorIndex, string playerName)
    {
        while (NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            yield return null;

        if (detachedCameraHolder != null)
        {
            Destroy(detachedCameraHolder);
            detachedCameraHolder = null;
        }

        PlayerCustomization customization = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerCustomization>();
        if (customization != null)
        {
            customization.SetSelection(classIndex, colorIndex);
            if (!string.IsNullOrEmpty(playerName))
                customization.SetName(playerName);
        }
    }
}
