using TMPro;
using UnityEngine;

// Keeps the HUD name label in sync with the player's display name - same pattern as AvatarUI/BackgroundColorUI.
public class PlayerNameUI : MonoBehaviour
{
    private TextMeshProUGUI label;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        CharacterCustomizationMenu.OnNameChanged += SetName;

        // Initial sync in case the Customize menu hasn't been opened yet this session.
        SetName(CharacterCustomizationMenu.Instance?.GetCurrentName());
    }

    void OnDisable()
    {
        CharacterCustomizationMenu.OnNameChanged -= SetName;
    }

    private void SetName(string value)
    {
        if (label != null && !string.IsNullOrEmpty(value))
            label.text = value;
    }
}
