using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Wires the Lobby's MultiplayerPanel Host/Join buttons to real Netcode connections carried
/// over Unity Relay (via the Multiplayer Services Session API), so players can connect across the
/// internet without port-forwarding. The scene keeps its offline, non-networked player active by
/// default (so solo play and every existing interact/UI system just work) - only once Host/Join is
/// actually pressed does the offline player get replaced by a networked, Netcode-spawned one.</summary>
public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private MenuPanel multiplayerPanel;

    // Doubles as the relay join-code field: after hosting, the generated code is written into it
    // (read-only) so the host can read/copy it; a joining client types a code into it before pressing
    // Join. Left named "ipInputField" so the existing Inspector reference to the scene's IPInputField
    // object isn't lost.
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxPlayers = 4;

    // Join-flow feedback: disabled/greyed while a join attempt is in flight so the player can't double
    // click it into a second concurrent attempt, and a status label for "searching for the lobby" /
    // error text (JoinGame is the only flow that surfaces this - Host has no "did we find it?" wait).
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text joinStatusText;

    // Persistent in-game HUD readout (top-right) showing the relay code once hosting succeeds, so the
    // host can still see/share it after the MultiplayerPanel is closed - unlike ipInputField, this one
    // isn't shared with the Join flow's own placeholder/text.
    [SerializeField] private TMP_Text sessionCodeText;

    // Where the host's own player was standing right before pressing Host - ApprovalCheck uses this
    // instead of spawnPoint for the host's own connection, so hosting doesn't teleport them; joining
    // clients still spawn at spawnPoint since they have no prior position in this session to keep.
    private Vector3? hostSpawnPosition;
    private Quaternion hostSpawnRotation;

    // The offline player's "Main Camera"/"CinemachineCamera" children get reparented here right before
    // the player itself is deactivated, so the camera keeps rendering during session setup (which takes
    // real time) without keeping the whole player active - see CaptureOfflinePlayerState. Destroyed once
    // the networked replacement (which brings its own camera pair) actually spawns.
    private GameObject detachedCameraHolder;

    // NetworkManager.Singleton is set in NetworkManager's own Awake(), and Unity doesn't guarantee
    // Awake() order between different components on the same GameObject - Start() does guarantee
    // every Awake() in the scene has already run, so wire up here instead.
    // Connection approval must be enabled (and the callback assigned) before StartHost/StartClient
    // runs, so it has to happen here rather than inside HostGame/JoinGame.
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

    // Runs on the server for every connecting client, including the host's own local connection.
    // Setting Position here spawns the player prefab already in place - the player's own
    // NetworkTransform (AuthorityMode = Owner) reports that position out to everyone else, so unlike
    // a post-spawn reposition it works uniformly for the host and for joining clients alike.
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;

        // The host's own connection always uses client id 0 (NetworkManager.ServerClientId) in
        // client-server topology - keep it at hostSpawnPosition (where it stood before Host was
        // pressed) instead of the shared spawnPoint used for actual joining clients.
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

    // The offline player is a plain instance of the same prefab NetworkManager will spawn - and, like
    // every scene-placed copy of a NetworkObject-bearing prefab, Netcode treats it as an in-scene
    // network object that gets auto-spawned as a phantom extra "player" the moment the server actually
    // starts (see NetworkConfig.EnableSceneManagement). It must therefore be deactivated immediately,
    // synchronously, before any Host/Join network call - not once the replacement spawns. Its camera
    // rig is detached first so the screen doesn't go dark for the (real) time session setup takes.
    // Returns its current class/color/name so the caller can carry them over to the networked
    // replacement, which otherwise spawns back at PlayerCustomization's defaults.
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

    // Netcode's default player-prefab auto-spawn always creates a fresh instance at
    // PlayerCustomization's defaults - reapply whatever the offline player was wearing/named right
    // before Host/Join replaced it with this networked one.
    private System.Collections.IEnumerator RestoreLocalCustomizationWhenSpawned(int classIndex, int colorIndex, string playerName)
    {
        while (NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            yield return null;

        // The networked replacement brings its own camera pair - the detached one from the old offline
        // player has served its purpose (bridging the gap while session setup was in progress).
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
