using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Server-authoritative wipe check: once every connected player's health hits zero, send everyone
// back to the Lobby - PlayerStats.ResetToFull already revives them the moment they arrive there.
public class GameOverCheck : NetworkBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private float startupGracePeriod = 0.5f;
    private bool hasTriggeredGameOver;
    private float readyTime;

    void Start()
    {
        readyTime = Time.time + startupGracePeriod;
    }

    void Update()
    {
        if (!this.HasServerAuthority() || hasTriggeredGameOver || Time.time < readyTime)
            return;

        if (!AllPlayersDead())
            return;

        hasTriggeredGameOver = true;
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworked)
            NetworkManager.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        else
            LoadingScreenManager.LoadScene(lobbySceneName);
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
