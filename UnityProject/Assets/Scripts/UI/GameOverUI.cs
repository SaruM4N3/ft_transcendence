using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Game over screen: the host picks Restart or Return to Lobby, everyone else waits.
public class GameOverUI : MonoBehaviour
{
    private const float ClosedScale = 0.85f;

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform window;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private TMP_Text waitingText;
    [SerializeField] private float showDelay = 0.3f;
    [SerializeField] private float fadeDuration = 0.25f;

    private GameOverCheck check;
    private CanvasGroup panelGroup;
    private float triggerTime = -1f;
    private bool shown;

    public static bool IsShowing { get; private set; }

    void Awake()
    {
        IsShowing = false;
        panelGroup = panel.GetComponent<CanvasGroup>();
        panel.SetActive(false);

        restartButton.onClick.AddListener(() => Choose(restart: true));
        lobbyButton.onClick.AddListener(() => Choose(restart: false));
    }

    void OnDestroy()
    {
        IsShowing = false;
    }

    // Waits a beat after the wipe so the last death is visible before the screen appears.
    // Polled (not event-driven) so a restart's fresh GameOverCheck is picked up automatically.
    void Update()
    {
        if (check == null)
            check = FindAnyObjectByType<GameOverCheck>();

        bool isOver = check != null && check.IsGameOver;
        if (!isOver)
        {
            if (shown || triggerTime >= 0f)
                Reset();
            return;
        }

        if (shown)
            return;

        if (triggerTime < 0f)
            triggerTime = Time.unscaledTime;

        if (Time.unscaledTime - triggerTime >= showDelay)
            Show();
    }

    // Restart/ReturnToLobby spawn a fresh GameOverCheck with gameOver back at false; close and re-arm for it.
    private void Reset()
    {
        shown = false;
        IsShowing = false;
        triggerTime = -1f;
        panel.SetActive(false);
        PauseManager.SetExternalPause(false);
        restartButton.interactable = true;
        lobbyButton.interactable = true;
    }

    private void Show()
    {
        shown = true;
        IsShowing = true;
        PauseManager.SetExternalPause(true);

        bool canChoose = check.CanChoose;
        restartButton.gameObject.SetActive(canChoose);
        lobbyButton.gameObject.SetActive(canChoose);
        waitingText.gameObject.SetActive(!canChoose);

        panel.SetActive(true);
        StartCoroutine(FadeIn());
    }

    private void Choose(bool restart)
    {
        restartButton.interactable = false;
        lobbyButton.interactable = false;

        if (restart)
            check.Restart();
        else
            check.ReturnToLobby();
    }

    // Unscaled time, so it plays whatever Time.timeScale is.
    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / fadeDuration), 3f);
            window.localScale = Vector3.one * Mathf.Lerp(ClosedScale, 1f, eased);
            panelGroup.alpha = eased;
            yield return null;
        }

        window.localScale = Vector3.one;
        panelGroup.alpha = 1f;
    }
}
