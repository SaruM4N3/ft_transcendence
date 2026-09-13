using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Server-authoritative wipe check: once every connected player's health hits zero, send everyone
// back to the Lobby - PlayerStats.ResetToFull already revives them the moment they arrive there.
public class GameOverCheck : NetworkBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";

    private bool hasTriggeredGameOver;

    void Update()
    {
        if (!IsServer || hasTriggeredGameOver)
            return;

        if (PlayerCustomization.AllActiveInstances.Count == 0)
            return;

        foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats == null || stats.CurrentHealth > 0f)
                return;
        }

        hasTriggeredGameOver = true;
        NetworkManager.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }
}
