using System.Collections;
using UnityEngine;
using TMPro;

// One NPC's own "press E" prompt - a World Space Canvas at a fixed local position above it, so it
// moves for free via the Transform hierarchy instead of needing runtime screen-position math.
public class InteractPromptUI : MonoBehaviour
{
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TextMeshProUGUI keyLabel;
    [SerializeField] private TextMeshProUGUI actionLabel;
    [SerializeField] private string keyGlyph = "E";
    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private float closedScale = 0.85f;
    [SerializeField] private float keyCapBlinkSpeed = 3f;
    [SerializeField] private float keyCapBlinkMinAlpha = 0.35f;
    [SerializeField] private float keyCapBlinkScaleAmplitude = 0.1f;

    private CanvasGroup canvasGroup;
    private RectTransform keyCap;
    private CanvasGroup keyCapGroup;
    private Coroutine activeAnimation;
    private Coroutine blinkAnimation;

    private void Awake()
    {
        if (keyLabel != null)
            keyLabel.text = keyGlyph;

        if (promptRoot != null)
        {
            canvasGroup = promptRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = promptRoot.gameObject.AddComponent<CanvasGroup>();

            promptRoot.localScale = Vector3.one * closedScale;
            canvasGroup.alpha = 0f;
            promptRoot.gameObject.SetActive(false);

            // Keycap badge blinks on its own while shown, fading/scaling as one unit.
            keyCap = promptRoot.Find("KeyCap") as RectTransform;
            if (keyCap != null)
            {
                keyCapGroup = keyCap.GetComponent<CanvasGroup>();
                if (keyCapGroup == null)
                    keyCapGroup = keyCap.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    public void Show(string text)
    {
        if (actionLabel != null)
            actionLabel.text = text;

        if (promptRoot == null)
            return;

        promptRoot.gameObject.SetActive(true);
        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(Animate(opening: true));

        if (keyCap != null && blinkAnimation == null)
            blinkAnimation = StartCoroutine(BlinkKeyCap());
    }

    public void Hide()
    {
        if (promptRoot == null || !promptRoot.gameObject.activeSelf)
            return;

        if (blinkAnimation != null)
        {
            StopCoroutine(blinkAnimation);
            blinkAnimation = null;
            keyCap.localScale = Vector3.one;
            keyCapGroup.alpha = 1f;
        }

        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(Animate(opening: false));
    }

    // Same unscaled-time scale+alpha tween as MenuPanel's Open/Close, so a prompt can fade out even while paused.
    private IEnumerator Animate(bool opening)
    {
        float fromScale = opening ? closedScale : 1f;
        float toScale = opening ? 1f : closedScale;
        float fromAlpha = opening ? 0f : 1f;
        float toAlpha = opening ? 1f : 0f;

        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.unscaledDeltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / animationDuration), 3f);
            promptRoot.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            yield return null;
        }

        promptRoot.localScale = Vector3.one * toScale;
        canvasGroup.alpha = toAlpha;

        if (!opening)
            promptRoot.gameObject.SetActive(false);

        activeAnimation = null;
    }

    // Continuous alpha+scale pulse on the keycap to draw the eye to the key to press.
    private IEnumerator BlinkKeyCap()
    {
        while (true)
        {
            float wave = (Mathf.Sin(Time.unscaledTime * keyCapBlinkSpeed) + 1f) * 0.5f;
            keyCapGroup.alpha = Mathf.Lerp(keyCapBlinkMinAlpha, 1f, wave);
            keyCap.localScale = Vector3.one * Mathf.Lerp(1f - keyCapBlinkScaleAmplitude, 1f, wave);
            yield return null;
        }
    }
}
