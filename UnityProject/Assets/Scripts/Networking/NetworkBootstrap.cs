using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Wires the Lobby's MultiplayerPanel Host/Join buttons to Netcode over Unity Relay, so
/// players can connect across the internet without port-forwarding. The scene's offline player stays
/// active by default for solo play - only Host/Join replaces it with a networked, Netcode-spawned one.</summary>
public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private MenuPanel multiplayerPanel;

    // Also the relay join-code field: hosting writes the generated code into it (read-only) for the
    // host to copy; a joining client types a code in before pressing Join. Named ipInputField to match
    // the existing Inspector wiring.
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxPlayers = 4;

    // Join-flow feedback: disabled while a join is in flight (no double-click), plus a status label for
    // "searching"/error text. Host has no equivalent wait, so no matching fields there.
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text joinStatusText;

    // Top-right HUD readout showing the relay code after hosting, so it's still visible once
    // MultiplayerPanel closes - separate from ipInputField, which the Join flow also uses.
    [SerializeField] private TMP_Text sessionCodeText;

    // Captured pre-Host so ApprovalCheck can spawn the host back where they stood instead of at
    // spawnPoint (used for actual joining clients, who have no prior position to keep).
    private Vector3? hostSpawnPosition;
    private Quaternion hostSpawnRotation;

    // Holds the offline player's camera while it's reparented off during session setup - see
    // CaptureOfflinePlayerState. Destroyed once the networked replacement spawns with its own camera.
    private GameObject detachedCameraHolder;

    // NetworkManager.Singleton is only set in its own Awake(), and Awake order across components isn't
    // guaranteed - Start() runs after every Awake(), so connection approval is wired up here instead.
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

    // Runs on the server for every connecting client, including the host's own. Setting Position here
    // spawns the player prefab already in place, which the owner's own NetworkTransform then reports
    // out to everyone else.
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;

        // The host's own connection is always client id 0 in client-server topology.
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
        // The code doesn't exist until CreateSessionAsync (relay allocation) returns, which takes a
        // moment - show a placeholder in the field it will land in rather than leaving it blank.
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

            // CreateSessionAsync with a relay network allocates the relay, wires the NetworkManager's
            // UnityTransport with the relay server data, and starts Netcode as host - no manual
            // NetworkManager.Singleton.StartHost() call needed.
            SessionOptions options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
            IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);

            if (ipInputField != null)
            {
                ipInputField.text = session.Code;
                ipInputField.interactable = false;
            }
            if (sessionCodeText != null)
                // Only the code itself switches to Liberation Sans (more legible for a string players
                // read/type back) - "Code:" stays in the HUD's usual display font.
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

            // JoinSessionByCodeAsync with the host's relay code wires the UnityTransport and starts
            // Netcode as client - no manual NetworkManager.Singleton.StartClient() call needed.
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
            // Always re-enable, including on success: the panel is closing anyway, and this is what
            // leaves the controls usable the next time the panel is opened.
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

    // The offline player carries a live NetworkObject (same prefab NetworkManager spawns), so Netcode
    // auto-spawns it as a phantom extra "player" the moment the server starts unless it's deactivated
    // first - synchronously, before any Host/Join call, not once the replacement spawns. Its camera is
    // detached first so the screen doesn't go dark while session setup is in progress. Returns its
    // current class/color/name so the caller can carry them over to the replacement.
    private (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) CaptureOfflinePlayerState()
    {
        GameObject offlinePlayer = GameObject.FindWithTag("Player");
        if (offlinePlayer == null)
            return (0, 0, string.Empty, Vector3.zero, Quaternion.identity);

        PlayerCustomization customization = offlinePlayer.GetComponent<PlayerCustomization>();
        (int classIndex, int colorIndex, string playerName) current = customization != null
            ? (customization.ClassIndex, customization.ColorIndex, customization.PlayerName)
            : (0, 0, string.Empty);
        Vector3 position = offlinePlayer.transform.position;
        Quaternion rotation = offlinePlayer.transform.rotation;

        Transform mainCamera = offlinePlayer.transform.Find("Main Camera");
        Transform cinemachineCamera = offlinePlayer.transform.Find("CinemachineCamera");
        if (mainCamera != null || cinemachineCamera != null)
        {
            detachedCameraHolder = new GameObject("TemporaryCameraHolder");
            if (mainCamera != null)
                mainCamera.SetParent(detachedCameraHolder.transform, worldPositionStays: true);
            if (cinemachineCamera != null)
                cinemachineCamera.SetParent(detachedCameraHolder.transform, worldPositionStays: true);
        }

        offlinePlayer.SetActive(false);
        return (current.classIndex, current.colorIndex, current.playerName, position, rotation);
    }

    // Netcode's auto-spawned player always starts at PlayerCustomization's defaults - reapply whatever
    // the offline player was wearing/named before Host/Join replaced it.
    private System.Collections.IEnumerator RestoreLocalCustomizationWhenSpawned(int classIndex, int colorIndex, string playerName)
    {
        while (NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            yield return null;

        // The replacement brings its own camera pair - the detached one has served its purpose.
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
