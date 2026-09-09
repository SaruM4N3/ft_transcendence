using UnityEngine;
using TMPro;

/// <summary>One NPC's own "press E" prompt - a World Space Canvas sitting at a fixed local position
/// above that specific NPC, so it moves with it for free via the Transform hierarchy and never needs
/// runtime screen-position math (unlike the old single roaming Screen Space prompt).</summary>
public class InteractPromptUI : MonoBehaviour
{
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TextMeshProUGUI keyLabel;
    [SerializeField] private TextMeshProUGUI actionLabel;
    [SerializeField] private string keyGlyph = "E";

    private void Awake()
    {
        if (keyLabel != null)
            keyLabel.text = keyGlyph;

        if (promptRoot != null)
            promptRoot.gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        if (actionLabel != null)
            actionLabel.text = text;

        if (promptRoot != null)
            promptRoot.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (promptRoot != null)
            promptRoot.gameObject.SetActive(false);
    }
}
