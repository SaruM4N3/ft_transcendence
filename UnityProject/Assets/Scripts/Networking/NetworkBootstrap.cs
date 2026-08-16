using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

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

    // NetworkManager.Singleton is set in NetworkManager's own Awake(), and Unity doesn't guarantee
    // Awake() order between different components on the same GameObject - Start() does guarantee
    // every Awake() in the scene has already run, so subscribe here instead.
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    public async void HostGame()
    {
        try
        {
            await EnsureSignedInAsync();

            (int classIndex, int colorIndex) customization = DeactivateOfflinePlayer();

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

            // The host is also a client of its own server, but OnClientConnectedCallback isn't reliable
            // for that self-connection - trigger the reposition directly instead of depending on it here.
            if (spawnPoint != null)
                StartCoroutine(RepositionWhenSpawned(NetworkManager.Singleton.LocalClientId));
            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to host - {e.Message}");
        }
    }

    public async void JoinGame()
    {
        string code = ipInputField != null ? ipInputField.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("NetworkBootstrap: enter a join code before joining.");
            return;
        }

        try
        {
            await EnsureSignedInAsync();

            (int classIndex, int colorIndex) customization = DeactivateOfflinePlayer();

            // JoinSessionByCodeAsync with the host's relay code wires the UnityTransport and starts
            // Netcode as client - no manual NetworkManager.Singleton.StartClient() call needed.
            await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            multiplayerPanel.Close();

            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to join - {e.Message}");
        }
    }

    private static async System.Threading.Tasks.Task EnsureSignedInAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // The default NetworkManager player-prefab auto-spawn ignores the scene's own spawn point, so
    // the server (host) repositions every newly connected player's spawned object once it exists.
    // OnClientConnectedCallback can fire before that client's PlayerObject finishes spawning
    // (reliably true for the host's own connection), so wait for it rather than bailing immediately.
    private void HandleClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer || spawnPoint == null)
            return;

        StartCoroutine(RepositionWhenSpawned(clientId));
    }

    private System.Collections.IEnumerator RepositionWhenSpawned(ulong clientId)
    {
        NetworkClient client;
        while (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out client) || client.PlayerObject == null)
            yield return null;

        client.PlayerObject.transform.position = spawnPoint.position;
    }

    // The offline player is a plain (non-networked) instance of the same prefab NetworkManager will
    // spawn - deactivate it so the incoming networked instance doesn't end up sharing the scene with
    // a duplicate player/camera/audio listener. Returns its current class/color so the caller can
    // carry it over to the networked replacement, which otherwise spawns back at
    // PlayerCustomization's defaults.
    private (int classIndex, int colorIndex) DeactivateOfflinePlayer()
    {
        GameObject offlinePlayer = GameObject.FindWithTag("Player");
        if (offlinePlayer == null)
            return (0, 0);

        PlayerCustomization customization = offlinePlayer.GetComponent<PlayerCustomization>();
        (int classIndex, int colorIndex) current = customization != null
            ? (customization.ClassIndex, customization.ColorIndex)
            : (0, 0);

        offlinePlayer.SetActive(false);
        return current;
    }

    // Netcode's default player-prefab auto-spawn always creates a fresh instance at
    // PlayerCustomization's defaults - reapply whatever the offline player was wearing right before
    // Host/Join replaced it with this networked one.
    private System.Collections.IEnumerator RestoreLocalCustomizationWhenSpawned(int classIndex, int colorIndex)
    {
        while (NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            yield return null;

        PlayerCustomization customization = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerCustomization>();
        if (customization != null)
            customization.SetSelection(classIndex, colorIndex);
    }
}
