using TMPro;
using UnityEngine;

/// <summary>Keeps the HUD's top-left name label in sync with the player's chosen display name -
/// same pattern as AvatarUI/BackgroundColorUI.</summary>
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

        // Initial sync in case the Customize menu hasn't been opened yet this session, same reasoning
        // as AvatarUI's initial portrait sync.
        CharacterCustomizationMenu menu = FindAnyObjectByType<CharacterCustomizationMenu>(FindObjectsInactive.Include);
        if (menu != null)
            SetName(menu.GetCurrentName());
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
