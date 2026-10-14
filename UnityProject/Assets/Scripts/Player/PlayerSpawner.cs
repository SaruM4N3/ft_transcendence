using Unity.Netcode;
using UnityEngine;

// Spawns the offline solo player at PlayerSpawnPoint.
[DefaultExecutionOrder(-100)]
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private void Start()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
            return;

        if (GameObject.FindWithTag("Player") != null)
            return;

        PlayerSpawnPoint spawnPoint = FindAnyObjectByType<PlayerSpawnPoint>();
        Vector3 position = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;
        Quaternion rotation = spawnPoint != null ? spawnPoint.transform.rotation : Quaternion.identity;

        Instantiate(playerPrefab, position, rotation);
    }
}
