using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One row of the multiplayer lobby roster (LobbyRosterUI) - avatar, name and health bar for
/// a single player. Unlike AvatarUI/PlayerNameUI/StatBarUI (which listen to the local-only static
/// events and always represent the local player), an entry is explicitly Bind()'d to one specific
/// PlayerCustomization/PlayerStats pair, so it can just as well represent another player.</summary>
public class LobbyRosterEntryUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image healthFillImage;

    private PlayerCustomization boundCustomization;
    private PlayerStats boundStats;

    public void Bind(PlayerCustomization customization, PlayerStats stats)
    {
        Unbind();

        boundCustomization = customization;
        boundStats = stats;

        customization.OnDisplayNameChanged += SetName;
        customization.OnClassOrColorChanged += SetAvatar;
        if (stats != null)
            stats.OnHealthReplicated += SetHealth;

        SetName(customization.CurrentDisplayName);
        SetAvatar(customization.ClassIndex, customization.ColorIndex);
        if (stats != null)
            SetHealth(stats.CurrentHealth, stats.MaxHealth);
    }

    public void Unbind()
    {
        if (boundCustomization != null)
        {
            boundCustomization.OnDisplayNameChanged -= SetName;
            boundCustomization.OnClassOrColorChanged -= SetAvatar;
        }
        if (boundStats != null)
            boundStats.OnHealthReplicated -= SetHealth;

        boundCustomization = null;
        boundStats = null;
    }

    private void OnDestroy() => Unbind();

    private void SetName(string value)
    {
        if (nameText != null)
            nameText.text = value;
    }

    private void SetAvatar(int classIndex, int colorIndex)
    {
        if (avatarImage == null || CharacterCustomizationMenu.Instance == null)
            return;

        Sprite portrait = CharacterCustomizationMenu.Instance.GetPortrait(classIndex, colorIndex);
        if (portrait != null)
            avatarImage.sprite = portrait;
    }

    private void SetHealth(float current, float max)
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = max > 0f ? current / max : 0f;
    }
}
