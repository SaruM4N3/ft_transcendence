using System.Collections.Generic;
using UnityEngine;

// Top-left roster with one row per connected player.
public class LobbyRosterUI : MonoBehaviour
{
    [SerializeField] private RectTransform rowTemplate;
    [SerializeField] private float rowSpacing = 60f;

    private readonly List<PlayerCustomization> orderedPlayers = new List<PlayerCustomization>();
    private readonly Dictionary<PlayerCustomization, LobbyRosterEntryUI> rows =
        new Dictionary<PlayerCustomization, LobbyRosterEntryUI>();

    private void Awake()
    {
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerCustomization.OnPlayerRegistered += HandlePlayerRegistered;
        PlayerCustomization.OnPlayerUnregistered += HandlePlayerUnregistered;

        foreach (PlayerCustomization existing in PlayerCustomization.AllActiveInstances)
            HandlePlayerRegistered(existing);
    }

    private void OnDisable()
    {
        PlayerCustomization.OnPlayerRegistered -= HandlePlayerRegistered;
        PlayerCustomization.OnPlayerUnregistered -= HandlePlayerUnregistered;

        foreach (LobbyRosterEntryUI row in rows.Values)
            if (row != null)
                Destroy(row.gameObject);
        rows.Clear();
        orderedPlayers.Clear();
    }

    private void HandlePlayerRegistered(PlayerCustomization player)
    {
        if (rowTemplate == null || rows.ContainsKey(player) || player.IsOwner)
            return;

        GameObject rowObject = Instantiate(rowTemplate.gameObject, rowTemplate.parent);
        rowObject.SetActive(true);
        LobbyRosterEntryUI entry = rowObject.GetComponent<LobbyRosterEntryUI>();
        entry.Bind(player, player.GetComponent<PlayerStats>());

        rows[player] = entry;
        orderedPlayers.Add(player);
        RelayoutRows();
    }

    private void HandlePlayerUnregistered(PlayerCustomization player)
    {
        if (!rows.TryGetValue(player, out LobbyRosterEntryUI entry))
            return;

        if (entry != null)
            Destroy(entry.gameObject);
        rows.Remove(player);
        orderedPlayers.Remove(player);
        RelayoutRows();
    }

    // Rows keep join order so the list doesn't reshuffle.
    private void RelayoutRows()
    {
        for (int i = 0; i < orderedPlayers.Count; i++)
        {
            if (!rows.TryGetValue(orderedPlayers[i], out LobbyRosterEntryUI entry) || entry == null)
                continue;

            RectTransform rect = entry.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -i * rowSpacing);
        }
    }
}
