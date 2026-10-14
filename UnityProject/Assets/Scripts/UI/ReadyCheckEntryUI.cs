using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One ready-check row: avatar, name, team and ready status.
public class ReadyCheckEntryUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI readyStatusText;
    [SerializeField] private TextMeshProUGUI teamText;

    private static readonly Color ReadyColor = new Color(0.3f, 0.85f, 0.3f);
    private static readonly Color NotReadyColor = new Color(0.85f, 0.3f, 0.3f);

    private Player boundPlayer;
    private string[] teamNames;

    public void Bind(Player player, string[] teamNames = null)
    {
        Unbind();

        this.teamNames = teamNames;
        boundPlayer = player;
        player.OnDisplayNameChanged += SetName;
        player.OnClassOrColorChanged += SetAvatar;
        player.OnReadyChanged += SetReady;
        player.OnTeamChanged += SetTeam;

        SetName(player.CurrentDisplayName);
        SetAvatar(player.ClassIndex, player.ColorIndex);
        SetReady(player.IsReady);
        SetTeam(player.TeamIndex);
    }

    public void Unbind()
    {
        if (boundPlayer != null)
        {
            boundPlayer.OnDisplayNameChanged -= SetName;
            boundPlayer.OnClassOrColorChanged -= SetAvatar;
            boundPlayer.OnReadyChanged -= SetReady;
            boundPlayer.OnTeamChanged -= SetTeam;
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
