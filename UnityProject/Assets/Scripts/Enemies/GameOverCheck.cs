using System.Collections.Generic;
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
    private bool sceneReady;
    private float readyTime;

    public bool IsGameOver => IsSpawned ? gameOver.Value : offlineGameOver;

    public bool CanChoose => this.HasServerAuthority();

    // Networked: waits for every client's scene load to finish before evaluating wipes, so stale health can't false-trigger it.
    void Start()
    {
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworked && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
        else
            ArmReadiness();
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        base.OnDestroy();
    }

    private void HandleLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        ArmReadiness();
    }

    private void ArmReadiness()
    {
        sceneReady = true;
        readyTime = Time.time + startupGracePeriod;
    }

    void Update()
    {
        if (!this.HasServerAuthority() || IsGameOver || !sceneReady || Time.time < readyTime)
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
        if (Player.AllActiveInstances.Count == 0)
        {
            GameObject localPlayer = LocalPlayer.Get();
            Player localInstance = localPlayer != null ? localPlayer.GetComponent<Player>() : null;
            return localInstance != null && localInstance.CurrentHealth <= 0f;
        }

        foreach (Player player in Player.AllActiveInstances)
        {
            if (player.CurrentHealth > 0f)
                return false;
        }
        return true;
    }
}
