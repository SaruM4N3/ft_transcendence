using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps the HUD bottom-left background sword in sync with the player's current color.</summary>
public class BackgroundColorUI : MonoBehaviour
{
    private Image backgroundImage;

    void Awake()
    {
        backgroundImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        CharacterCustomizationMenu.OnBackgroundChanged += SetBackground;

        // Initial sync in case the Customize menu hasn't been opened yet this session.
        SetBackground(CharacterCustomizationMenu.Instance?.GetCurrentBackground());
    }

    void OnDisable()
    {
        CharacterCustomizationMenu.OnBackgroundChanged -= SetBackground;
    }

    private void SetBackground(Sprite sprite)
    {
        if (sprite != null)
            backgroundImage.sprite = sprite;
    }
}
