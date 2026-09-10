using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

// Persistent watcher: when a session ends under us (host left or dropped), falls back to an offline Lobby.
public class SessionDisconnectHandler : MonoBehaviour
{
    private const string LobbySceneName = "Lobby";
    private const float ExpectedStopWindow = 5f;

    private static float expectedStopUntil;
    private static bool quitting;

    private NetworkManager subscribedManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("SessionDisconnectHandler");
        DontDestroyOnLoad(go);
        go.AddComponent<SessionDisconnectHandler>();
    }

    // Hosts delete the session; a failed call never blocks leaving. Stops are flagged so they aren't treated as drops.
    public static async Task LeaveSessionsAsync()
    {
        expectedStopUntil = Time.unscaledTime + ExpectedStopWindow;

        foreach (ISession session in MultiplayerService.Instance.Sessions.Values.ToList())
        {
            try
            {
                if (session.IsHost)
                    await session.AsHost().DeleteAsync();
                else
                    await session.LeaveAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SessionDisconnectHandler: couldn't leave session cleanly - {e.Message}");
            }
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    void Update()
    {
        if (subscribedManager == NetworkManager.Singleton)
            return;

        if (subscribedManager != null)
            subscribedManager.OnClientStopped -= HandleClientStopped;

        subscribedManager = NetworkManager.Singleton;
        if (subscribedManager != null)
            subscribedManager.OnClientStopped += HandleClientStopped;
    }

    void OnApplicationQuit()
    {
        quitting = true;
    }

    void OnDestroy()
    {
        if (subscribedManager != null)
            subscribedManager.OnClientStopped -= HandleClientStopped;
    }

    private void HandleClientStopped(bool wasHost)
    {
        if (quitting || Time.unscaledTime < expectedStopUntil)
            return;

        StartCoroutine(ReturnToOfflineLobbyNextFrame());
    }

    // Deferred a frame so the scene load doesn't run inside Netcode's own shutdown callback.
    private IEnumerator ReturnToOfflineLobbyNextFrame()
    {
        yield return null;
        _ = LeaveStaleSessionsThenLoadLobbyAsync();
    }

    private static async Task LeaveStaleSessionsThenLoadLobbyAsync()
    {
        await LeaveSessionsAsync();
        LoadingScreenManager.LoadScene(LobbySceneName);
    }
}
