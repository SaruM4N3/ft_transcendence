using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Server-authoritative horde spawner: waves of enemies appear in a ring around this object and
// chase the players down; the next wave starts once the current one is fully cleared.
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
    private int waveNumber;
    private bool wavesStarted;

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
            waveNumber++;
            int count = baseEnemiesPerWave + (waveNumber - 1) * extraEnemiesPerWave;
            SpawnWave(count);

            yield return new WaitUntil(AllEnemiesDead);
            yield return new WaitForSeconds(timeBetweenWaves);
        }
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

    // Never returns a water or blocked point: after all attempts fail it falls back to the spawner's own (safe) position.
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

    // The enemy's collider sits above its pivot, so spawn checks must test where the body will actually be.
    private Vector2 GetEnemyBodyOffset()
    {
        CircleCollider2D circle = enemyPrefab.GetComponent<CircleCollider2D>();
        return circle != null ? circle.offset * (Vector2)enemyPrefab.transform.localScale : Vector2.zero;
    }

    // Land at the point and at its four sides (so no coast-edge spawns) with no static collider on it.
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
