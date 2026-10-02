using Unity.Netcode;
using UnityEngine;

// Server-side shared XP/level tracker for a Coop run; XP required per level scales with player count so the shared bar isn't easier to fill with more players.
public class XPManager : NetworkBehaviour
{
    [SerializeField] private int baseXpPerLevel = 50;
    [SerializeField] private int xpPerLevelIncrement = 25;

    private readonly NetworkVariable<int> totalXP = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int offlineXP;
    private bool subscribed;

    public int TotalXP => IsSpawned ? totalXP.Value : offlineXP;

    void Start()
    {
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isNetworked)
            Subscribe();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            Subscribe();
    }

    public override void OnNetworkDespawn()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    // Guarded so an early defensive OnNetworkDespawn (fires even offline, before Start() subscribes) can't wipe out the offline subscription.
    private void Subscribe()
    {
        if (subscribed)
            return;
        subscribed = true;
        EnemyStats.OnEnemyKilled += HandleEnemyKilled;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;
        subscribed = false;
        EnemyStats.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleEnemyKilled(int xpAmount)
    {
        if (IsSpawned)
            totalXP.Value += xpAmount;
        else
            offlineXP += xpAmount;
    }

    // Walks the level curve fresh each call - cheap at these level counts, and stays correct if player count changed mid-run.
    public void GetProgress(out int level, out int xpIntoLevel, out int xpForLevel)
    {
        int playerCount = Mathf.Max(1, Player.AllActiveInstances.Count);
        int remaining = TotalXP;

        level = 1;
        int requirement = XpRequiredForLevel(level, playerCount);
        while (remaining >= requirement)
        {
            remaining -= requirement;
            level++;
            requirement = XpRequiredForLevel(level, playerCount);
        }

        xpIntoLevel = remaining;
        xpForLevel = requirement;
    }

    private int XpRequiredForLevel(int level, int playerCount)
    {
        return (baseXpPerLevel + (level - 1) * xpPerLevelIncrement) * playerCount;
    }
}
