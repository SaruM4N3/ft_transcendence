using System.Collections;
using UnityEngine;
using TMPro;

// Per-NPC world-space "press E" prompt at a fixed local position.
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
        RefreshKeyLabel();

        if (promptRoot != null)
        {
            canvasGroup = promptRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = promptRoot.gameObject.AddComponent<CanvasGroup>();

            promptRoot.localScale = Vector3.one * closedScale;
            canvasGroup.alpha = 0f;
            promptRoot.gameObject.SetActive(false);

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
        RefreshKeyLabel();

        if (actionLabel != null)
            actionLabel.text = text;

        if (promptRoot == null)
            return;

        promptRoot.gameObject.SetActive(true);
        if (!promptRoot.gameObject.activeInHierarchy)
            return;

        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(Animate(opening: true));

        if (keyCap != null && blinkAnimation == null)
            blinkAnimation = StartCoroutine(BlinkKeyCap());
    }

    // Reflects the player's current Interact keybind, including rebinds; falls back to keyGlyph if unresolved.
    private void RefreshKeyLabel()
    {
        if (keyLabel != null)
            keyLabel.text = KeybindOverrides.GetKeyboardDisplay("Interact") ?? keyGlyph;
    }

    public void Hide()
    {
        // activeInHierarchy, not activeSelf: a deactivated parent (e.g. mountInteractRoot on revive) leaves
        // this object's own flag true while StartCoroutine below would still fail.
        if (promptRoot == null || !promptRoot.gameObject.activeInHierarchy)
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

    // Unscaled-time scale and alpha tween, so it works while paused.
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

    // Pulses the keycap's alpha and scale.
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
