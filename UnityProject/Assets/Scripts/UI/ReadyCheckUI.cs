using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Ready check controller: opens ReadyCheckPanel when anyone proposes a mode (ModeReadyCheck/GameModeLoader),
// lists ready status, lets the local player toggle theirs. Lives on an always-active object separate from
// the panel, since MenuPanel.Close() deactivates its own GameObject and would kill these subscriptions.
public class ReadyCheckUI : MonoBehaviour
{
    [SerializeField] private MenuPanel menuPanel;
    [SerializeField] private RectTransform rowTemplate;
    [SerializeField] private float rowSpacing = 60f;
    [SerializeField] private TextMeshProUGUI modeNameText;
    [SerializeField] private TextMeshProUGUI readyButtonLabel;

    // Maps GameModeLoader's raw scene name to a friendly label; falls back to the raw name if unlisted.
    private static readonly Dictionary<string, string> ModeDisplayNames = new Dictionary<string, string>
    {
        { "Coop", "4-Player Co-op" },
    };

    private bool isOpen;
    private readonly List<PlayerCustomization> orderedPlayers = new List<PlayerCustomization>();
    private readonly Dictionary<PlayerCustomization, ReadyCheckEntryUI> rows =
        new Dictionary<PlayerCustomization, ReadyCheckEntryUI>();

    private void Awake()
    {
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (ModeReadyCheck.Instance != null)
            ModeReadyCheck.Instance.OnPendingSceneChanged += HandlePendingSceneChanged;
        PlayerCustomization.OnPlayerRegistered += HandlePlayerRegistered;
        PlayerCustomization.OnPlayerUnregistered += HandlePlayerUnregistered;
    }

    private void OnDisable()
    {
        if (ModeReadyCheck.Instance != null)
            ModeReadyCheck.Instance.OnPendingSceneChanged -= HandlePendingSceneChanged;
        PlayerCustomization.OnPlayerRegistered -= HandlePlayerRegistered;
        PlayerCustomization.OnPlayerUnregistered -= HandlePlayerUnregistered;

        ClearRows();
    }

    private void HandlePendingSceneChanged(string sceneName)
    {
        isOpen = !string.IsNullOrEmpty(sceneName);

        if (!isOpen)
        {
            menuPanel.Close();
            return;
        }

        if (modeNameText != null)
            modeNameText.text = ModeDisplayNames.TryGetValue(sceneName, out string displayName) ? displayName : sceneName;

        RebuildRows();
        UpdateReadyButtonLabel();
        menuPanel.Open();
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

    // Handles a player joining while a check is already showing (RebuildRows covers the "check just started" case).
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
        entry.Bind(player);

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
