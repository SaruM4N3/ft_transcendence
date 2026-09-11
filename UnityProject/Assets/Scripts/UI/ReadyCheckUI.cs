using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>Mode-select ready check controller: opens ReadyCheckPanel on every client's screen when
/// anyone proposes a mode (see ModeReadyCheck/GameModeLoader), lists each player's ready status, and
/// lets the local player toggle their own. Lives on an always-active object separate from the panel it
/// controls - MenuPanel.Close() deactivates its own GameObject, which would otherwise kill this
/// script's subscriptions and prevent it from ever reopening.</summary>
public class ReadyCheckUI : MonoBehaviour
{
    [SerializeField] private MenuPanel menuPanel;
    [SerializeField] private RectTransform rowTemplate;
    [SerializeField] private float rowSpacing = 60f;
    [SerializeField] private TextMeshProUGUI modeNameText;
    [SerializeField] private TextMeshProUGUI readyButtonLabel;

    // GameModeLoader only ever has the raw scene name to pass along (that's what the mode buttons are
    // wired with) - this maps it to the friendly label the buttons themselves show, for modeNameText.
    // Falls back to the raw scene name for anything not listed here yet (e.g. once 2v2/3v1 get real
    // scenes wired up).
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

    // Handles a player joining/leaving while a check is already showing - RebuildRows above covers the
    // "check just started" case.
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

    /// <summary>Wired to the panel's Ready button - toggles the local player's own readiness.</summary>
    public void ToggleReady()
    {
        PlayerCustomization customization = GetLocalPlayerCustomization();
        if (customization == null)
            return;

        customization.SetReady(!customization.IsReady);
        UpdateReadyButtonLabel();
    }

    private void UpdateReadyButtonLabel()
    {
        if (readyButtonLabel == null)
            return;

        PlayerCustomization customization = GetLocalPlayerCustomization();
        readyButtonLabel.text = customization != null && customization.IsReady ? "Cancel" : "Ready";
    }

    private static PlayerCustomization GetLocalPlayerCustomization()
    {
        GameObject player = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null
            ? NetworkManager.Singleton.LocalClient.PlayerObject?.gameObject
            : null;
        return player != null ? player.GetComponent<PlayerCustomization>() : null;
    }
}
