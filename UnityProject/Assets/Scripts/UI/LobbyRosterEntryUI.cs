using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One roster row, explicitly bound to a player.
public class LobbyRosterEntryUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image healthFillImage;

    private Player boundPlayer;

    public void Bind(Player player)
    {
        Unbind();

        boundPlayer = player;

        player.OnDisplayNameChanged += SetName;
        player.OnClassOrColorChanged += SetAvatar;
        player.OnHealthReplicated += SetHealth;

        SetName(player.CurrentDisplayName);
        SetAvatar(player.ClassIndex, player.ColorIndex);
        SetHealth(player.CurrentHealth, player.MaxHealth);
    }

    public void Unbind()
    {
        if (boundPlayer != null)
        {
            boundPlayer.OnDisplayNameChanged -= SetName;
            boundPlayer.OnClassOrColorChanged -= SetAvatar;
            boundPlayer.OnHealthReplicated -= SetHealth;
        }

        boundPlayer = null;
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
