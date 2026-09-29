using System.Collections.Generic;
using UnityEngine;

// Top-left roster with one row per connected player.
public class LobbyRosterUI : MonoBehaviour
{
    [SerializeField] private RectTransform rowTemplate;
    [SerializeField] private float rowSpacing = 60f;

    private readonly List<Player> orderedPlayers = new List<Player>();
    private readonly Dictionary<Player, LobbyRosterEntryUI> rows =
        new Dictionary<Player, LobbyRosterEntryUI>();

    private void Awake()
    {
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Player.OnPlayerRegistered += HandlePlayerRegistered;
        Player.OnPlayerUnregistered += HandlePlayerUnregistered;

        foreach (Player existing in Player.AllActiveInstances)
            HandlePlayerRegistered(existing);
    }

    private void OnDisable()
    {
        Player.OnPlayerRegistered -= HandlePlayerRegistered;
        Player.OnPlayerUnregistered -= HandlePlayerUnregistered;

        foreach (LobbyRosterEntryUI row in rows.Values)
            if (row != null)
                Destroy(row.gameObject);
        rows.Clear();
        orderedPlayers.Clear();
    }

    private void HandlePlayerRegistered(Player player)
    {
        if (rowTemplate == null || rows.ContainsKey(player) || player.IsOwner)
            return;

        GameObject rowObject = Instantiate(rowTemplate.gameObject, rowTemplate.parent);
        rowObject.SetActive(true);
        LobbyRosterEntryUI entry = rowObject.GetComponent<LobbyRosterEntryUI>();
        entry.Bind(player);

        rows[player] = entry;
        orderedPlayers.Add(player);
        RelayoutRows();
    }

    private void HandlePlayerUnregistered(Player player)
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
