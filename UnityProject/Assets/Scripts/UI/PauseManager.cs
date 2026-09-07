using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    private const float ButtonSpacing = 45f;

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button quitButton;

    public static bool IsPaused { get; private set; }

    private static bool IsOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private bool InLobby => SceneManager.GetActiveScene().name == lobbySceneName;

    // Everyone except a mode-scene host leaves the session.
    private bool LeavesSession => IsOnline && (InLobby || !NetworkManager.Singleton.IsServer);

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

    // Host in a mode scene brings everyone to the Lobby; others leave the session first.
    public void LeaveOrReturn()
    {
        bool hostBringsEveryoneBack = IsOnline && !LeavesSession;
        SetPaused(false);

        if (hostBringsEveryoneBack)
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        else
            _ = LeaveSessionThenLoadLobbyAsync();
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Used by NPC menu panels: blocks input via IsPaused but leaves Time.timeScale running.
    public static void SetExternalPause(bool paused)
    {
        IsPaused = paused;
    }

    private async Task LeaveSessionThenLoadLobbyAsync()
    {
        if (IsOnline)
            await LeaveSessionAsync();

        LoadingScreenManager.LoadScene(lobbySceneName);
    }

    // Hosts delete the session; a failed call never blocks leaving.
    private static async Task LeaveSessionAsync()
    {
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
                Debug.LogWarning($"PauseManager: couldn't leave session cleanly - {e.Message}");
            }
        }

        if (IsOnline)
            NetworkManager.Singleton.Shutdown();
    }

    // Shows the leave button when there is somewhere to go; hides Quit on WebGL.
    private void RefreshButtons()
    {
        bool showLeave = !InLobby || IsOnline;
        bool showQuit = Application.platform != RuntimePlatform.WebGLPlayer;

        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(showLeave);
            TMP_Text label = leaveButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = LeavesSession ? "Leave Session" : "Return to Lobby";
        }

        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(showQuit);
            RectTransform rect = (RectTransform)quitButton.transform;
            float y = showLeave ? -2f * ButtonSpacing : -ButtonSpacing;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }
    }

    // Multiplayer pause only blocks input; freezing timeScale would desync.
    private void SetPaused(bool paused)
    {
        IsPaused = paused;

        bool isMultiplayerSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isMultiplayerSession)
            Time.timeScale = paused ? 0f : 1f;

        if (paused)
            RefreshButtons();

        if (pausePanel != null)
            pausePanel.SetActive(paused);
    }
}
