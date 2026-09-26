using System.Collections.Generic;
using UnityEngine;

// Server-side hub shared by every enemy: the living-player list and one flow field per chased player.
public class EnemyFlowFieldManager : MonoBehaviour
{
    private const float PlayerRefreshInterval = 0.25f;
    private const float RebuildInterval = 0.5f;
    private const float ForcedRebuildInterval = 2f;
    private const float UnusedFieldLifetime = 2f;

    private static EnemyFlowFieldManager instance;

    private readonly List<PlayerStats> livingPlayers = new List<PlayerStats>();
    private readonly List<PlayerStats> scratchPlayers = new List<PlayerStats>();
    private readonly Dictionary<PlayerStats, EnemyFlowField> fields = new Dictionary<PlayerStats, EnemyFlowField>();
    private readonly List<PlayerStats> staleKeys = new List<PlayerStats>();
    private float nextPlayerRefreshTime;

    public int PlayersVersion { get; private set; }

    public static Vector2 BodyOffset { get; set; }

    public static EnemyFlowFieldManager Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("EnemyFlowFieldManager").AddComponent<EnemyFlowFieldManager>();
            return instance;
        }
    }

    void OnEnable()
    {
        PlayerStats.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        PlayerStats.OnPlayerDied -= HandlePlayerDied;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void FixedUpdate()
    {
        if (Time.time >= nextPlayerRefreshTime)
            RefreshPlayers();

        UpdateFields();
    }

    public PlayerStats FindNearestLivingPlayer(Vector2 position)
    {
        PlayerStats nearest = null;
        float nearestDistSq = float.MaxValue;
        foreach (PlayerStats player in livingPlayers)
        {
            float distSq = ((Vector2)player.transform.position - position).sqrMagnitude;
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = player;
            }
        }
        return nearest;
    }

    // Marks the target's field as in use and samples it; false while it is not built yet or out of range.
    public bool TryGetDirection(PlayerStats target, Vector2 position, out Vector2 direction)
    {
        direction = Vector2.zero;
        if (!fields.TryGetValue(target, out EnemyFlowField field))
        {
            field = new EnemyFlowField();
            fields.Add(target, field);
        }

        field.LastRequestedAt = Time.time;
        return field.IsBuilt && field.TryGetDirection(position, out direction);
    }

    private void HandlePlayerDied(PlayerStats dead)
    {
        RefreshPlayers();
    }

    // Bumps PlayersVersion only when the living set actually changed, so enemies re-aggro on deaths and revives.
    private void RefreshPlayers()
    {
        nextPlayerRefreshTime = Time.time + PlayerRefreshInterval;
        scratchPlayers.Clear();

        // Solo play never spawns PlayerCustomization (no session), so the registry stays empty -
        // fall back to the one local player, same resolution ProceduralMapGenerator uses.
        if (PlayerCustomization.AllActiveInstances.Count == 0)
        {
            GameObject localPlayer = LocalPlayer.Get();
            PlayerStats localStats = localPlayer != null ? localPlayer.GetComponent<PlayerStats>() : null;
            if (localStats != null && !localStats.IsDead)
                scratchPlayers.Add(localStats);
        }
        else
        {
            foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
            {
                PlayerStats stats = player.GetComponent<PlayerStats>();
                if (stats != null && !stats.IsDead)
                    scratchPlayers.Add(stats);
            }
        }

        if (SameContents(scratchPlayers, livingPlayers))
            return;

        livingPlayers.Clear();
        livingPlayers.AddRange(scratchPlayers);
        PlayersVersion++;
    }

    private static bool SameContents(List<PlayerStats> a, List<PlayerStats> b)
    {
        if (a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }

    // Drops unused or dead-player fields, then rebuilds the most out-of-date one(s) within the per-step budget.
    private void UpdateFields()
    {
        float now = Time.time;
        staleKeys.Clear();
        PlayerStats mostDue = null;
        float mostDueAge = 0f;

        foreach (KeyValuePair<PlayerStats, EnemyFlowField> pair in fields)
        {
            PlayerStats player = pair.Key;
            EnemyFlowField field = pair.Value;
            if (player == null || player.IsDead || now - field.LastRequestedAt > UnusedFieldLifetime)
            {
                staleKeys.Add(player);
                continue;
            }

            float age = now - field.BuiltAt;
            bool due = !field.IsBuilt
                || (age >= RebuildInterval && EnemyFlowField.ToCell((Vector2)player.transform.position + BodyOffset) != field.GoalCell)
                || age >= ForcedRebuildInterval;
            if (due && (mostDue == null || age > mostDueAge))
            {
                mostDue = player;
                mostDueAge = field.IsBuilt ? age : float.MaxValue;
            }
        }

        foreach (PlayerStats key in staleKeys)
            fields.Remove(key);

        if (mostDue != null)
            fields[mostDue].Rebuild((Vector2)mostDue.transform.position + BodyOffset);
    }
}
