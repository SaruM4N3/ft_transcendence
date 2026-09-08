using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// One friends-panel row: name, tag, and up to two action buttons.
public class FriendRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text tagText;
    [SerializeField] private Button primaryButton;
    [SerializeField] private TMP_Text primaryLabel;
    [SerializeField] private Button secondaryButton;
    [SerializeField] private TMP_Text secondaryLabel;

    public void Setup(string displayName, string tag, string primaryText, Action onPrimary, string secondaryText = null, Action onSecondary = null)
    {
        nameText.text = displayName;
        tagText.text = tag;
        tagText.color = TagColor(tag);
        ConfigureButton(primaryButton, primaryLabel, primaryText, onPrimary);
        ConfigureButton(secondaryButton, secondaryLabel, secondaryText, onSecondary);
    }

    // Online green, Request yellow, otherwise red.
    private static Color TagColor(string tag)
    {
        switch (tag)
        {
            case "Online": return new Color(0.2f, 0.7f, 0.25f);
            case "Request": return new Color(0.95f, 0.75f, 0.1f);
            default: return new Color(0.8f, 0.15f, 0.15f);
        }
    }

    private static void ConfigureButton(Button button, TMP_Text label, string text, Action onClick)
    {
        bool visible = onClick != null;
        button.gameObject.SetActive(visible);
        button.onClick.RemoveAllListeners();
        if (!visible)
            return;

        label.text = text;
        button.onClick.AddListener(new UnityAction(onClick));
    }
}
