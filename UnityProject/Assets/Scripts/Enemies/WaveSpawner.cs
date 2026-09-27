using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Server-side wave spawner; the next wave starts when the current one is cleared.
public class WaveSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int baseEnemiesPerWave = 3;
    [SerializeField] private int extraEnemiesPerWave = 2;
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private int maxSpawnPointAttempts = 30;
    [SerializeField] private float spawnClearance = 1f;

    private readonly List<NetworkObject> aliveEnemies = new List<NetworkObject>();
    private readonly NetworkVariable<int> currentWave = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<double> startServerTime = new NetworkVariable<double>(
        0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int waveNumber;
    private float offlineStartTime;
    private bool wavesStarted;

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
        while (true)
        {
            if (waveNumber == 0)
                MarkStartTime();

            waveNumber++;
            if (IsSpawned)
                currentWave.Value = waveNumber;

            int count = baseEnemiesPerWave + (waveNumber - 1) * extraEnemiesPerWave;
            SpawnWave(count);

            yield return new WaitUntil(AllEnemiesDead);
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private void MarkStartTime()
    {
        if (IsSpawned)
            startServerTime.Value = NetworkManager.ServerTime.Time;
        else
            offlineStartTime = Time.time;
    }

    private bool AllEnemiesDead()
    {
        aliveEnemies.RemoveAll(enemy => enemy == null);
        return aliveEnemies.Count == 0;
    }

    private void SpawnWave(int count)
    {
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(enemyPrefab, FindSpawnPosition(), Quaternion.identity);
            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            if (isNetworked)
                netObj.Spawn(true);
            aliveEnemies.Add(netObj);
        }
    }

    // Falls back to the spawner's own position if no valid point is found.
    private Vector3 FindSpawnPosition()
    {
        ProceduralMapGenerator mapGenerator = FindAnyObjectByType<ProceduralMapGenerator>();

        for (int attempt = 0; attempt < maxSpawnPointAttempts; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            Vector3 candidate = transform.position + (Vector3)offset;

            if (IsValidSpawnPoint(candidate, mapGenerator))
                return candidate;
        }

        return transform.position;
    }

    // The collider sits above the pivot, so spawn checks test the body position.
    private Vector2 GetEnemyBodyOffset()
    {
        CircleCollider2D circle = enemyPrefab.GetComponent<CircleCollider2D>();
        return circle != null ? circle.offset * (Vector2)enemyPrefab.transform.localScale : Vector2.zero;
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
