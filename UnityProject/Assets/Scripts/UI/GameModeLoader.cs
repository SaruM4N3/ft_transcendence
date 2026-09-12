using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        bool isMultiplayerSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isMultiplayerSession && ModeReadyCheck.Instance != null)
        {
            // Broadcasts a ready check instead of loading immediately - ModeReadyCheck loads the scene for everyone once ready.
            ModeReadyCheck.Instance.RequestReadyCheck(sceneName);
            return;
        }

        Time.timeScale = 1f;
        PauseManager.SetExternalPause(false);
        SceneManager.LoadScene(sceneName);
    }
}
