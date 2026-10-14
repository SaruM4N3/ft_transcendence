using UnityEngine;
using UnityEngine.UI;

// Keeps the HUD sword in sync with the player's color.
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
