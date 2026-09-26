using System.Reflection;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

// Wires the Lobby's Host/Join buttons to Netcode over Unity Relay. The scene's offline player stays
// active for solo play - only Host/Join replaces it with a networked, Netcode-spawned one.
public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private MenuPanel multiplayerPanel;
    [SerializeField] private GameObject gameModeManagerPrefab;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text joinStatusText;
    [SerializeField] private TMP_Text sessionCodeText;
    private Vector3? hostSpawnPosition;
    private Quaternion hostSpawnRotation;
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

    public void HostGame()
    {
        _ = HostGameAsync();
    }

    // Shared by the Host button and friend invites; returns false if hosting failed or a session is already running.
    public async System.Threading.Tasks.Task<bool> HostGameAsync()
    {
        if (NetworkManager.Singleton.IsListening)
            return false;

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

        // Can't host twice, and can't join your own session once hosting - re-enabled on failure below.
        if (hostButton != null)
            hostButton.interactable = false;
        SetJoinControlsInteractable(false);

        try
        {
            await ServicesAuth.EnsureSignedInAsync();

            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState();
            hostSpawnPosition = customization.position;
            hostSpawnRotation = customization.rotation;

            // Allocates the relay, wires UnityTransport, and starts Netcode as host - no manual StartHost() needed.
            SessionOptions options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
            IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);

            if (gameModeManagerPrefab != null)
            {
                GameObject gameModeManager = Instantiate(gameModeManagerPrefab);
                gameModeManager.GetComponent<NetworkObject>().Spawn();
            }

            if (ipInputField != null)
            {
                ipInputField.text = session.Code;
                ipInputField.interactable = false;
            }
            if (sessionCodeText != null)
                // Liberation Sans is more legible for a code players read/type back; "Code:" keeps the HUD font.
                sessionCodeText.text = $"Code: <font=\"LiberationSans SDF\">{session.Code}</font>";

            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex, customization.playerName));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to host - {e.Message}");
            if (placeholderText != null)
                placeholderText.text = originalPlaceholder;
            if (hostButton != null)
                hostButton.interactable = true;
            SetJoinControlsInteractable(true);
            return false;
        }
    }

    public async void JoinGame()
    {
        string code = ipInputField != null ? ipInputField.text.Trim() : string.Empty;
        await JoinWithCodeAsync(code);
    }

    // Shared by the Join button and the friend-invite toast, which supplies a code that didn't come from the input field.
    public async System.Threading.Tasks.Task<bool> JoinWithCodeAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            SetJoinStatus("Enter a join code before joining.", isError: true);
            return false;
        }

        if (NetworkManager.Singleton.IsListening)
            return false;

        SetJoinControlsInteractable(false);
        SetJoinStatus("Searching for the lobby...", isError: false);

        // Can't host once joined - re-enabled below only if the join attempt actually fails.
        if (hostButton != null)
            hostButton.interactable = false;

        try
        {
            await ServicesAuth.EnsureSignedInAsync();

            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState();

            // Wires UnityTransport and starts Netcode as client - no manual StartClient() needed.
            await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            SetJoinStatus(string.Empty, isError: false);
            if (multiplayerPanel.gameObject.activeInHierarchy)
                multiplayerPanel.Close();

            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex, customization.playerName));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to join - {e.Message}");
            SetJoinStatus("Couldn't find that lobby - check the code and try again.", isError: true);
            if (hostButton != null)
                hostButton.interactable = true;
            return false;
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
