using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuPanel : MonoBehaviour
{
    public static MenuPanel CurrentOpen { get; private set; }

    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private float closedScale = 0.85f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine activeAnimation;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        PauseManager.SetExternalPause(true);
        CurrentOpen = this;

        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(Animate(opening: true));
    }

    public void Close()
    {
        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(Animate(opening: false));
    }

    // Runs on unscaled time so it still plays while Time.timeScale is 0 (the menu's own pause).
    private IEnumerator Animate(bool opening)
    {
        float fromScale = opening ? closedScale : 1f;
        float toScale = opening ? 1f : closedScale;
        float fromAlpha = opening ? 0f : 1f;
        float toAlpha = opening ? 1f : 0f;

        canvasGroup.blocksRaycasts = opening;
        canvasGroup.interactable = opening;

        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.unscaledDeltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / animationDuration), 3f);
            rectTransform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            yield return null;
        }

        rectTransform.localScale = Vector3.one * toScale;
        canvasGroup.alpha = toAlpha;

        if (!opening)
        {
            gameObject.SetActive(false);
            PauseManager.SetExternalPause(false);
            if (CurrentOpen == this)
                CurrentOpen = null;
        }

        activeAnimation = null;
    }
}
