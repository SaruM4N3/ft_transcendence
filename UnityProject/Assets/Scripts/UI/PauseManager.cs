using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public static bool IsPaused { get; private set; }

    void Awake()
    {
        IsPaused = false;
    }

    public void Pause(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;

        if (MenuPanel.CurrentOpen != null)
        {
            MenuPanel.CurrentOpen.Close();
            return;
        }

        if (IsPaused)
            Resume();
        else
            SetPaused(true);
    }

    public void Resume()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        SetPaused(false);
    }

    public void OpenSettings()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        LoadingScreenManager.LoadScene(mainMenuSceneName);
    }

    // Used by NPC menu panels: blocks input via IsPaused but leaves Time.timeScale running.
    public static void SetExternalPause(bool paused)
    {
        IsPaused = paused;
    }

    private void SetPaused(bool paused)
    {
        IsPaused = paused;

        // Freezing Time.timeScale would desync a networked session, so multiplayer pause only blocks local input.
        bool isMultiplayerSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isMultiplayerSession)
            Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
            pausePanel.SetActive(paused);
    }
}
