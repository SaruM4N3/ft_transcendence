using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        PauseManager.SetExternalPause(false);
        SceneManager.LoadScene(sceneName);
    }
}
