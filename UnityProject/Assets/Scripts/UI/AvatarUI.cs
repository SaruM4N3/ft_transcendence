using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps the HUD portrait in sync with the player's current class/color.</summary>
public class AvatarUI : MonoBehaviour
{
    private Image avatarImage;

    void Awake()
    {
        avatarImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        CharacterCustomizationMenu.OnPortraitChanged += SetAvatar;

        // Initial sync in case the Customize menu hasn't been opened yet this session (so it never
        // fired OnPortraitChanged) - find it directly (even inactive) and ask it to resolve the
        // portrait, rather than reading the player's raw (animating) world sprite.
        CharacterCustomizationMenu menu = FindAnyObjectByType<CharacterCustomizationMenu>(FindObjectsInactive.Include);
        if (menu != null)
            SetAvatar(menu.GetCurrentPortrait());
    }

    void OnDisable()
    {
        CharacterCustomizationMenu.OnPortraitChanged -= SetAvatar;
    }

    private void SetAvatar(Sprite portrait)
    {
        if (portrait != null)
            avatarImage.sprite = portrait;
    }
}
