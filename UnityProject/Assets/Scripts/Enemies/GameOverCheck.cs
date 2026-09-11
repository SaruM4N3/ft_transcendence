using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Detects a full wipe and lets the host (or solo player) restart the mode or return to the Lobby.
public class GameOverCheck : NetworkBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private float startupGracePeriod = 0.5f;

    private readonly NetworkVariable<bool> gameOver = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool offlineGameOver;
    private bool isLoading;
    private float readyTime;

    public bool IsGameOver => IsSpawned ? gameOver.Value : offlineGameOver;

    public bool CanChoose => this.HasServerAuthority();

    void Start()
    {
        readyTime = Time.time + startupGracePeriod;
    }

    void Update()
    {
        if (!this.HasServerAuthority() || IsGameOver || Time.time < readyTime)
            return;

        if (!AllPlayersDead())
            return;

        if (IsSpawned)
            gameOver.Value = true;
        else
            offlineGameOver = true;
    }

    public void Restart()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToLobby()
    {
        LoadScene(lobbySceneName);
    }

    // Only the host or a solo player may change scene, and only once.
    private void LoadScene(string sceneName)
    {
        if (!CanChoose || isLoading)
            return;

        isLoading = true;
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworked)
            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        else
            LoadingScreenManager.LoadScene(sceneName);
    }

    private bool AllPlayersDead()
    {
        if (PlayerCustomization.AllActiveInstances.Count == 0)
        {
            GameObject localPlayer = LocalPlayer.Get();
            PlayerStats localStats = localPlayer != null ? localPlayer.GetComponent<PlayerStats>() : null;
            return localStats != null && localStats.CurrentHealth <= 0f;
        }

        foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats == null || stats.CurrentHealth > 0f)
                return false;
        }
        return true;
    }
}
