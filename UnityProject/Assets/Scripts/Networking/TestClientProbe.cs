#if UNITY_STANDALONE && !UNITY_EDITOR
using System.Linq;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

// TEMPORARY: auto-joins via -testJoinCode and logs the client's view.
public class TestClientProbe : MonoBehaviour
{
    private string joinCode;
    private bool hasJoined;
    private bool hasReadied;
    private bool servicesReady;
    private bool eventsHooked;
    private float nextLogTime;

    // Sets the profile before any scene script signs in under the default one.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static async void InitServicesWithProfile()
    {
        foreach (string arg in System.Environment.GetCommandLineArgs())
        {
            if (!arg.StartsWith("-testJoinCode="))
                continue;
            var options = new Unity.Services.Core.InitializationOptions().SetProfile("TestClientProbe");
            await Unity.Services.Core.UnityServices.InitializeAsync(options);
            return;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        string code = null;
        foreach (string arg in System.Environment.GetCommandLineArgs())
            if (arg.StartsWith("-testJoinCode="))
                code = arg.Substring("-testJoinCode=".Length);

        if (code == null)
            return;

        var go = new GameObject("TestClientProbe");
        DontDestroyOnLoad(go);
        var probe = go.AddComponent<TestClientProbe>();
        probe.joinCode = code;
        Debug.Log($"[TestClientProbe] bootstrapped with code {code}");
    }

    async void Start()
    {
        await Unity.Services.Core.UnityServices.InitializeAsync();
        servicesReady = true;
    }

    // Logs the disconnect events a client receives when the host goes away.
    private void HookNetworkEvents()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (eventsHooked || manager == null)
            return;

        eventsHooked = true;
        manager.OnClientDisconnectCallback += id => Debug.Log($"[TestClientProbe] OnClientDisconnect id={id} reason='{manager.DisconnectReason}'");
        manager.OnClientStopped += wasHost => Debug.Log($"[TestClientProbe] OnClientStopped wasHost={wasHost}");
        manager.OnTransportFailure += () => Debug.Log("[TestClientProbe] OnTransportFailure");
    }

    void Update()
    {
        HookNetworkEvents();

        if (!hasJoined && servicesReady && AuthenticationService.Instance.IsSignedIn && NetworkManager.Singleton != null)
        {
            var bootstrap = FindAnyObjectByType<NetworkBootstrap>(FindObjectsInactive.Include);
            if (bootstrap != null)
            {
                hasJoined = true;
                var field = typeof(NetworkBootstrap).GetField("ipInputField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var inputField = field.GetValue(bootstrap) as TMPro.TMP_InputField;
                if (inputField != null)
                    inputField.text = joinCode;
                bootstrap.JoinGame();
                Debug.Log($"[TestClientProbe] JoinGame() called with code {joinCode}");
            }
        }

        if (!hasReadied && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            hasReadied = true;
            var customization = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();
            customization?.SetReady(true);
            Debug.Log("[TestClientProbe] auto-readied");
        }

        if (Time.time >= nextLogTime)
        {
            nextLogTime = Time.time + 1f;
            LogState();
        }
    }

    private void LogState()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string[] players = Player.AllActiveInstances
            .Select(p =>
            {
                NetworkObject no = p.GetComponent<NetworkObject>();
                return $"{p.gameObject.name}(isPlayerObj={no?.IsPlayerObject},owner={no?.OwnerClientId},active={p.gameObject.activeInHierarchy})";
            })
            .ToArray();

        EnemyStats[] enemies = FindObjectsByType<EnemyStats>(FindObjectsSortMode.None);
        string enemyInfo = string.Join(",", enemies.Select(e => $"pos={e.transform.position} hp={e.CurrentHealth}"));

        GameObject localPlayer = LocalPlayer.Get();
        Player stats = localPlayer != null ? localPlayer.GetComponent<Player>() : null;

        GameObject[] activePlayerTagged = GameObject.FindGameObjectsWithTag("Player");
        string rawInfo = string.Join(",", activePlayerTagged.Select(g =>
        {
            NetworkObject gno = g.GetComponent<NetworkObject>();
            return $"{g.name}(active={g.activeSelf},IsSpawned={gno?.IsSpawned},IsOwner={gno?.IsOwner},Owner={gno?.OwnerClientId},IsPlayerObj={gno?.IsPlayerObject})";
        }));

        NetworkManager manager = NetworkManager.Singleton;
        CoopHUD hud = FindAnyObjectByType<CoopHUD>();
        string hudInfo = hud == null ? "none" : string.Join("/", hud.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => t.text));
        int sessions = Unity.Services.Multiplayer.MultiplayerService.Instance.Sessions.Count;
        string net = manager == null ? "nm=null" : $"nm[listening={manager.IsListening} connected={manager.IsConnectedClient} shuttingDown={manager.ShutdownInProgress} sessions={sessions}] hud={hudInfo}";
        Debug.Log($"[TestClientProbe] {net} scene={scene} players=[{string.Join(" | ", players)}] rawPlayerTagged=[{rawInfo}] enemyCount={enemies.Length} enemies=[{enemyInfo}] myHealth={stats?.CurrentHealth}/{stats?.MaxHealth} isDead={stats?.IsDead}");
    }
}
#endif
