using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Server-side wave spawner; waves fire on a timer, but end that wait early once the current wave is fully cleared.
public class WaveSpawner : NetworkBehaviour
{
    [SerializeField] private EnemyKit[] enemyKits;
    [SerializeField] private int[] tierUnlockWave = { 1, 4 };
    [SerializeField] private int baseEnemiesPerWave = 3;
    [SerializeField] private int extraEnemiesPerWave = 2;
    [SerializeField] private int extraEnemiesPerPlayer = 2;
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float clearCheckInterval = 0.5f;
    [SerializeField] private int maxSpawnPointAttempts = 30;
    [SerializeField] private float spawnClearance = 1f;

    private readonly NetworkVariable<int> currentWave = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<double> startServerTime = new NetworkVariable<double>(
        0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int waveNumber;
    private float offlineStartTime;
    private bool wavesStarted;
    private readonly List<int> unlockedKitIndices = new List<int>();

    public int CurrentWave => IsSpawned ? currentWave.Value : waveNumber;

    // Seconds since the first wave began, on the shared server clock when networked.
    public float ElapsedSeconds
    {
        get
        {
            if (CurrentWave <= 0)
                return 0f;

            return IsSpawned ? (float)(NetworkManager.ServerTime.Time - startServerTime.Value) : Time.time - offlineStartTime;
        }
    }

    void Start()
    {
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isNetworked)
            BeginWaves();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadEventCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadEventCompleted;
    }

    private void HandleSceneLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        BeginWaves();
    }

    private void BeginWaves()
    {
        if (wavesStarted)
            return;

        wavesStarted = true;
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        MarkStartTime();

        while (true)
        {
            waveNumber++;
            if (IsSpawned)
                currentWave.Value = waveNumber;

            int playerCount = Mathf.Max(1, Player.AllActiveInstances.Count);
            int count = baseEnemiesPerWave + (waveNumber - 1) * extraEnemiesPerWave + (playerCount - 1) * extraEnemiesPerPlayer;
            SpawnWave(count);

            yield return WaitForNextWave();
        }
    }

    // Waits out timeBetweenWaves, but ends early the moment every enemy from the current wave is dead.
    private IEnumerator WaitForNextWave()
    {
        WaitForSeconds checkDelay = new WaitForSeconds(clearCheckInterval);
        float elapsed = 0f;

        while (elapsed < timeBetweenWaves)
        {
            if (FindObjectsByType<EnemyStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 0)
                yield break;

            yield return checkDelay;
            elapsed += clearCheckInterval;
        }
    }

    private void MarkStartTime()
    {
        if (IsSpawned)
            startServerTime.Value = NetworkManager.ServerTime.Time;
        else
            offlineStartTime = Time.time;
    }

    // Public so EnemyAI can resolve the same kit by index on every peer (see EnemyAI.ApplyKitFromIndex).
    public EnemyKit GetKit(int index)
    {
        return index >= 0 && index < enemyKits.Length ? enemyKits[index] : null;
    }

    // Each enemy independently rolls a random kit from whatever tiers are unlocked this wave; SetNetworkedKitIndex must run before Spawn() so remote clients get it in the initial state.
    private void SpawnWave(int count)
    {
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        RefreshUnlockedKitIndices();

        for (int i = 0; i < count; i++)
        {
            int kitIndex = unlockedKitIndices[Random.Range(0, unlockedKitIndices.Count)];
            EnemyKit kit = GetKit(kitIndex);
            GameObject instance = Instantiate(kit.EnemyPrefab, FindSpawnPosition(), Quaternion.identity);
            instance.GetComponent<Enemy>().Initialize(kit);

            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            if (isNetworked)
            {
                instance.GetComponent<EnemyAI>().SetNetworkedKitIndex(kitIndex);
                netObj.Spawn(true);
            }
        }
    }

    // A kit's tier unlocks once waveNumber reaches tierUnlockWave[tier-1]; tiers past that array are never unlocked. Falls back to kit 0 if nothing qualifies yet.
    private void RefreshUnlockedKitIndices()
    {
        unlockedKitIndices.Clear();
        for (int i = 0; i < enemyKits.Length; i++)
        {
            int tier = Mathf.Max(1, enemyKits[i].Tier);
            int unlockWave = tier <= tierUnlockWave.Length ? tierUnlockWave[tier - 1] : int.MaxValue;
            if (waveNumber >= unlockWave)
                unlockedKitIndices.Add(i);
        }

        if (unlockedKitIndices.Count == 0)
            unlockedKitIndices.Add(0);
    }

    // Falls back to the spawner's own position if no valid point is found; public so EnemyAI can reuse it to recycle stray enemies.
    public Vector3 FindSpawnPosition()
    {
        ProceduralMapGenerator mapGenerator = FindAnyObjectByType<ProceduralMapGenerator>();
        Vector3 center = GetSpawnCenter();

        for (int attempt = 0; attempt < maxSpawnPointAttempts; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            Vector3 candidate = center + (Vector3)offset;

            if (IsValidSpawnPoint(candidate, mapGenerator))
                return candidate;
        }

        return center;
    }

    // Spawns just off a random living player's screen instead of the spawner's own (map-center) position.
    private Vector3 GetSpawnCenter()
    {
        EnemyFlowFieldManager manager = EnemyFlowFieldManager.Instance;
        manager.RefreshPlayers();
        IReadOnlyList<Player> living = manager.LivingPlayers;

        return living.Count > 0 ? living[Random.Range(0, living.Count)].transform.position : transform.position;
    }

    // The collider sits above the pivot, so spawn checks test the body position; all enemy variants share the same base collider setup, so any kit's prefab is representative.
    private Vector2 GetEnemyBodyOffset()
    {
        GameObject prefab = enemyKits.Length > 0 ? enemyKits[0].EnemyPrefab : null;
        CircleCollider2D circle = prefab != null ? prefab.GetComponent<CircleCollider2D>() : null;
        return circle != null ? circle.offset * (Vector2)prefab.transform.localScale : Vector2.zero;
    }

    // Valid when the point and its four sides are land with no static collider.
    private bool IsValidSpawnPoint(Vector3 root, ProceduralMapGenerator mapGenerator)
    {
        Vector3 point = root + (Vector3)GetEnemyBodyOffset();
        if (ObstacleQuery.IsBlocked(point, spawnClearance * 0.5f))
            return false;
        if (mapGenerator == null)
            return true;

        Vector3[] probes =
        {
            point,
            point + Vector3.right * spawnClearance, point + Vector3.left * spawnClearance,
            point + Vector3.up * spawnClearance, point + Vector3.down * spawnClearance,
        };
        foreach (Vector3 probe in probes)
        {
            if (mapGenerator.IsWaterAtWorldPosition(probe))
                return false;
        }
        return true;
    }
}
