using Unity.Netcode;
using UnityEngine;

public class GameModeLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        bool isMultiplayerSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isMultiplayerSession && ModeReadyCheck.Instance != null)
        {
            ModeReadyCheck.Instance.RequestReadyCheck(sceneName);
            return;
        }

        Time.timeScale = 1f;
        PauseManager.SetExternalPause(false);
        LoadingScreenManager.LoadScene(sceneName);
    }
}
