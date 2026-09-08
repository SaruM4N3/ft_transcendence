using System.Reflection;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

// Wires the Lobby's Host/Join buttons to Netcode over Unity Relay.
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

    // Start runs after every Awake, so NetworkManager.Singleton is set.
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

    // Shared by the Host button and invites; false on failure or if already hosting.
    public async System.Threading.Tasks.Task<bool> HostGameAsync()
    {
        if (NetworkManager.Singleton.IsListening)
            return false;

        string originalPlaceholder = null;
        TMP_Text placeholderText = ipInputField != null ? ipInputField.placeholder as TMP_Text : null;
        if (placeholderText != null)
        {
            originalPlaceholder = placeholderText.text;
            placeholderText.text = "Generating code...";
        }
        if (sessionCodeText != null)
            sessionCodeText.text = string.Empty;

        if (hostButton != null)
            hostButton.interactable = false;
        SetJoinControlsInteractable(false);

        try
        {
            await ServicesAuth.EnsureSignedInAsync();

            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState();
            hostSpawnPosition = customization.position;
            hostSpawnRotation = customization.rotation;

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

    // Shared by the Join button and invites, which supply their own code.
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

        if (hostButton != null)
            hostButton.interactable = false;

        try
        {
            await ServicesAuth.EnsureSignedInAsync();

            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState();

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

    // Deactivates the offline player before Host/Join, keeping its camera and customization.
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

    // Reapplies the offline player's customization to the auto-spawned player.
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
