using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Opens the ready-check panel on a proposal and lets the local player toggle ready.
public class ReadyCheckUI : MonoBehaviour
{
    [SerializeField] private MenuPanel menuPanel;
    [SerializeField] private RectTransform rowTemplate;
    [SerializeField] private float rowSpacing = 60f;
    [SerializeField] private TextMeshProUGUI modeNameText;
    [SerializeField] private TextMeshProUGUI readyButtonLabel;

    [SerializeField] private RectTransform teamButtonContainer;
    [SerializeField] private RectTransform teamButtonTemplate;
    [SerializeField] private float teamButtonSpacing = 160f;

    // Per-mode display metadata keyed by scene name; no TeamNames means no team picker.
    private readonly struct ModeInfo
    {
        public readonly string DisplayName;
        public readonly string[] TeamNames;

        public ModeInfo(string displayName, string[] teamNames = null)
        {
            DisplayName = displayName;
            TeamNames = teamNames;
        }
    }

    private static readonly Dictionary<string, ModeInfo> ModeInfos = new Dictionary<string, ModeInfo>
    {
        { "Coop", new ModeInfo("4-Player Co-op") },
    };

    private bool isOpen;
    private ModeReadyCheck subscribedInstance;
    private readonly List<PlayerCustomization> orderedPlayers = new List<PlayerCustomization>();
    private readonly Dictionary<PlayerCustomization, ReadyCheckEntryUI> rows =
        new Dictionary<PlayerCustomization, ReadyCheckEntryUI>();
    private readonly List<RectTransform> teamButtons = new List<RectTransform>();
    private string[] currentTeamNames;

    private void Awake()
    {
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
        if (teamButtonTemplate != null)
            teamButtonTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerCustomization.OnPlayerRegistered += HandlePlayerRegistered;
        PlayerCustomization.OnPlayerUnregistered += HandlePlayerUnregistered;
    }

    private void OnDisable()
    {
        if (subscribedInstance != null)
            subscribedInstance.OnPendingSceneChanged -= HandlePendingSceneChanged;
        subscribedInstance = null;
        PlayerCustomization.OnPlayerRegistered -= HandlePlayerRegistered;
        PlayerCustomization.OnPlayerUnregistered -= HandlePlayerUnregistered;

        ClearRows();
        RebuildTeamButtons(null);
    }

    // Polls until the dynamically spawned ModeReadyCheck exists.
    private void Update()
    {
        if (subscribedInstance == ModeReadyCheck.Instance)
            return;

        if (subscribedInstance != null)
            subscribedInstance.OnPendingSceneChanged -= HandlePendingSceneChanged;

        subscribedInstance = ModeReadyCheck.Instance;
        if (subscribedInstance != null)
            subscribedInstance.OnPendingSceneChanged += HandlePendingSceneChanged;
    }

    private void HandlePendingSceneChanged(string sceneName)
    {
        isOpen = !string.IsNullOrEmpty(sceneName);

        if (!isOpen)
        {
            currentTeamNames = null;
            menuPanel.Close();
            return;
        }

        ModeInfos.TryGetValue(sceneName, out ModeInfo modeInfo);
        if (modeNameText != null)
            modeNameText.text = string.IsNullOrEmpty(modeInfo.DisplayName) ? sceneName : modeInfo.DisplayName;

        currentTeamNames = modeInfo.TeamNames;
        RebuildRows();
        RebuildTeamButtons(modeInfo.TeamNames);
        UpdateReadyButtonLabel();
        menuPanel.Open();
    }

    private void RebuildTeamButtons(string[] teamNames)
    {
        foreach (RectTransform button in teamButtons)
            if (button != null)
                Destroy(button.gameObject);
        teamButtons.Clear();

        if (teamButtonContainer != null)
            teamButtonContainer.gameObject.SetActive(teamNames != null && teamNames.Length > 0);

        if (teamButtonTemplate == null || teamNames == null)
            return;

        for (int i = 0; i < teamNames.Length; i++)
        {
            int teamIndex = i;
            RectTransform buttonRect = Instantiate(teamButtonTemplate, teamButtonTemplate.parent);
            buttonRect.gameObject.SetActive(true);
            buttonRect.anchoredPosition = new Vector2(teamIndex * teamButtonSpacing, buttonRect.anchoredPosition.y);

            TextMeshProUGUI label = buttonRect.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = teamNames[teamIndex];

            Button button = buttonRect.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => LocalPlayer.GetCustomization()?.SetTeam(teamIndex));

            teamButtons.Add(buttonRect);
        }
    }

    private void RebuildRows()
    {
        ClearRows();
        foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
            AddRow(player);
    }

    private void ClearRows()
    {
        foreach (ReadyCheckEntryUI row in rows.Values)
            if (row != null)
                Destroy(row.gameObject);
        rows.Clear();
        orderedPlayers.Clear();
    }

    // Adds a row when a player joins during a check.
    private void HandlePlayerRegistered(PlayerCustomization player)
    {
        if (!isOpen || rows.ContainsKey(player))
            return;

        AddRow(player);
    }

    private void HandlePlayerUnregistered(PlayerCustomization player)
    {
        if (!rows.TryGetValue(player, out ReadyCheckEntryUI entry))
            return;

        if (entry != null)
            Destroy(entry.gameObject);
        rows.Remove(player);
        orderedPlayers.Remove(player);
        RelayoutRows();
    }

    private void AddRow(PlayerCustomization player)
    {
        if (rowTemplate == null)
            return;

        GameObject rowObject = Instantiate(rowTemplate.gameObject, rowTemplate.parent);
        rowObject.SetActive(true);
        ReadyCheckEntryUI entry = rowObject.GetComponent<ReadyCheckEntryUI>();
        entry.Bind(player, currentTeamNames);

        rows[player] = entry;
        orderedPlayers.Add(player);
        RelayoutRows();
    }

    private void RelayoutRows()
    {
        for (int i = 0; i < orderedPlayers.Count; i++)
        {
            if (!rows.TryGetValue(orderedPlayers[i], out ReadyCheckEntryUI entry) || entry == null)
                continue;

            RectTransform rect = entry.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -i * rowSpacing);
        }
    }

    public void ToggleReady()
    {
        PlayerCustomization customization = LocalPlayer.GetCustomization();
        if (customization == null)
            return;

        customization.SetReady(!customization.IsReady);
        UpdateReadyButtonLabel();
    }

    private void UpdateReadyButtonLabel()
    {
        if (readyButtonLabel == null)
            return;

        PlayerCustomization customization = LocalPlayer.GetCustomization();
        readyButtonLabel.text = customization != null && customization.IsReady ? "Cancel" : "Ready";
    }
}
