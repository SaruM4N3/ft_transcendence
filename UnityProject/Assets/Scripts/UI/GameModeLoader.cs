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
            // Every connected client needs to agree before the scene actually changes - broadcasts a
            // ready check instead of loading immediately. ModeReadyCheck itself loads the scene (via
            // Netcode's NetworkSceneManager, so everyone transitions together) once everyone's ready.
            ModeReadyCheck.Instance.RequestReadyCheck(sceneName);
            return;
        }

        Time.timeScale = 1f;
        PauseManager.SetExternalPause(false);
        SceneManager.LoadScene(sceneName);
    }
}
