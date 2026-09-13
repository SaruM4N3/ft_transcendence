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
    // Avoids landing a spawn point on water - see ProceduralMapGenerator.IsWaterAtWorldPosition.
    [SerializeField] private int maxSpawnPointAttempts = 10;

    private readonly List<NetworkObject> aliveEnemies = new List<NetworkObject>();
    private int waveNumber;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
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
        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(enemyPrefab, FindSpawnPosition(), Quaternion.identity);
            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            netObj.Spawn(true);
            aliveEnemies.Add(netObj);
        }
    }

    private Vector3 FindSpawnPosition()
    {
        ProceduralMapGenerator mapGenerator = FindAnyObjectByType<ProceduralMapGenerator>();
        Vector3 candidate = transform.position;

        for (int attempt = 0; attempt < maxSpawnPointAttempts; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            candidate = transform.position + (Vector3)offset;

            if (mapGenerator == null || !mapGenerator.IsWaterAtWorldPosition(candidate))
                return candidate;
        }

        // Ran out of attempts - better to place an enemy awkwardly than to hang the spawner.
        return candidate;
    }
}
