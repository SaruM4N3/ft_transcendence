using TMPro;
using UnityEngine;

// Keeps the HUD name label in sync with the display name.
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
