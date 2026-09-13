using System;
using Unity.Netcode;
using UnityEngine;

// Networked health for a wave enemy. Unlike PlayerStats, the "owner" is always the server (enemies
// are server-spawned by WaveSpawner), so the server can write currentHealth directly - no need to
// bounce damage back to a specific owning client the way player damage does.
public class EnemyStats : NetworkBehaviour, IDamageable, IHealthStats
{
    [SerializeField] private float maxHealth = 40f;

    private readonly NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth.Value;

    public event Action<float, float> OnHealthChanged;

    void Awake()
    {
        currentHealth.OnValueChanged += (_, newValue) => OnHealthChanged?.Invoke(newValue, maxHealth);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
    }

    // Callable from any client (whichever attacker's hitbox landed the hit).
    public void RequestDamage(float amount)
    {
        if (!NetworkObject.IsSpawned)
            return;

        RequestDamageServerRpc(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageServerRpc(float amount)
    {
        currentHealth.Value = Mathf.Max(0f, currentHealth.Value - amount);
        if (currentHealth.Value <= 0f)
            NetworkObject.Despawn(true);
    }
}
