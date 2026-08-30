using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Persistent loading screen shown across scene transitions
public class LoadingScreenManager : MonoBehaviour
{
    private const int SortingOrder = 9000;
    private const float FadeDuration = 0.15f;
    private const float MinimumVisibleTime = 0.3f;

    private static LoadingScreenManager instance;

    private CanvasGroup canvasGroup;
    private Image progressFill;
    private Coroutine fadeRoutine;
    private NetworkManager subscribedNetworkManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("LoadingScreenManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<LoadingScreenManager>();
    }

    private void Awake()
    {
        BuildUI();
    }

    private void Update()
    {
        if (subscribedNetworkManager == NetworkManager.Singleton)
            return;

        if (subscribedNetworkManager != null && subscribedNetworkManager.SceneManager != null)
            subscribedNetworkManager.SceneManager.OnSceneEvent -= HandleNetworkSceneEvent;

        subscribedNetworkManager = NetworkManager.Singleton;
        if (subscribedNetworkManager != null && subscribedNetworkManager.SceneManager != null)
            subscribedNetworkManager.SceneManager.OnSceneEvent += HandleNetworkSceneEvent;
    }

    public static void LoadScene(string sceneName)
    {
        if (instance != null)
            instance.StartCoroutine(instance.LoadSceneRoutine(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        Show();
        float shownAt = Time.unscaledTime;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            SetProgress(op.progress / 0.9f);
            yield return null;
        }
        SetProgress(1f);

        float remaining = MinimumVisibleTime - (Time.unscaledTime - shownAt);
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        op.allowSceneActivation = true;
        yield return op;
        Hide();
    }

    private void HandleNetworkSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.Load)
            Show();
        else if (sceneEvent.SceneEventType == SceneEventType.LoadComplete
            && sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
            Hide();
    }

    private void Show()
    {
        SetProgress(0f);
        Fade(1f);
    }

    private void Hide()
    {
        Fade(0f);
    }

    private void SetProgress(float value)
    {
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(value);
    }

    private void Fade(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        canvasGroup.blocksRaycasts = true;
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < FadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / FadeDuration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0f;
        fadeRoutine = null;
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("LoadingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGroup = canvasGO.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        var backgroundGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundGO.transform.SetParent(canvasGO.transform, false);
        var backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        backgroundGO.GetComponent<Image>().color = Color.black;

        var labelGO = new GameObject("LoadingLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(canvasGO.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 40f);
        labelRect.sizeDelta = new Vector2(400f, 60f);
        TextMeshProUGUI loadingLabel = labelGO.GetComponent<TextMeshProUGUI>();
        loadingLabel.text = "Loading...";
        loadingLabel.fontSize = 36f;
        loadingLabel.alignment = TextAlignmentOptions.Center;
        loadingLabel.color = Color.white;

        var barBackgroundGO = new GameObject("ProgressBarBackground", typeof(RectTransform), typeof(Image));
        barBackgroundGO.transform.SetParent(canvasGO.transform, false);
        var barBackgroundRect = barBackgroundGO.GetComponent<RectTransform>();
        barBackgroundRect.anchorMin = barBackgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        barBackgroundRect.anchoredPosition = new Vector2(0f, -20f);
        barBackgroundRect.sizeDelta = new Vector2(400f, 24f);
        barBackgroundGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        var barFillGO = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
        barFillGO.transform.SetParent(barBackgroundGO.transform, false);
        var barFillRect = barFillGO.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;
        progressFill = barFillGO.GetComponent<Image>();
        progressFill.color = new Color(0.85f, 0.2f, 0.2f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;
    }
}
