using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One row of the mode-select ready check (ReadyCheckUI) - avatar, name and ready/not-ready
/// status for a single player. Same Bind/Unbind pattern as LobbyRosterEntryUI.</summary>
public class ReadyCheckEntryUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI readyStatusText;
    // Optional - blank/hidden when the current mode declares no teams (ReadyCheckUI passes null names).
    [SerializeField] private TextMeshProUGUI teamText;

    private static readonly Color ReadyColor = new Color(0.3f, 0.85f, 0.3f);
    private static readonly Color NotReadyColor = new Color(0.85f, 0.3f, 0.3f);

    private PlayerCustomization boundCustomization;
    // Names for the pending mode's teams (index -> label), supplied by ReadyCheckUI; null/empty = no teams.
    private string[] teamNames;

    public void Bind(PlayerCustomization customization, string[] teamNames = null)
    {
        Unbind();

        this.teamNames = teamNames;
        boundCustomization = customization;
        customization.OnDisplayNameChanged += SetName;
        customization.OnClassOrColorChanged += SetAvatar;
        customization.OnReadyChanged += SetReady;
        customization.OnTeamChanged += SetTeam;

        SetName(customization.CurrentDisplayName);
        SetAvatar(customization.ClassIndex, customization.ColorIndex);
        SetReady(customization.IsReady);
        SetTeam(customization.TeamIndex);
    }

    public void Unbind()
    {
        if (boundCustomization != null)
        {
            boundCustomization.OnDisplayNameChanged -= SetName;
            boundCustomization.OnClassOrColorChanged -= SetAvatar;
            boundCustomization.OnReadyChanged -= SetReady;
            boundCustomization.OnTeamChanged -= SetTeam;
        }
        boundCustomization = null;
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

    private void SetReady(bool ready)
    {
        if (readyStatusText == null)
            return;

        readyStatusText.text = ready ? "Ready" : "Not Ready";
        readyStatusText.color = ready ? ReadyColor : NotReadyColor;
    }

    private void SetTeam(int index)
    {
        if (teamText == null)
            return;

        bool hasTeams = teamNames != null && teamNames.Length > 0;
        teamText.gameObject.SetActive(hasTeams);
        if (hasTeams)
            teamText.text = index >= 0 && index < teamNames.Length ? teamNames[index] : string.Empty;
    }
}
