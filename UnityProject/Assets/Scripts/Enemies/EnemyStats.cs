using System;
using Unity.Netcode;
using UnityEngine;

// Networked enemy health; the server owns it and writes directly.
public class EnemyStats : NetworkBehaviour, IDamageable, IHealthStats
{
    private EnemyKit kit;

    private readonly NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Assigned at spawn time by whoever instantiates this enemy (see Enemy.Initialize); never baked into the prefab.
    public EnemyKit Kit { get => kit; set => kit = value; }

    public float MaxHealth => kit.MaxHealth;
    public float CurrentHealth => currentHealth.Value;

    public event Action<float, float> OnHealthChanged;

    // Fired once, server/offline-authoritative side only, right before the enemy despawns.
    public static event Action<int> OnEnemyKilled;

    void Awake()
    {
        currentHealth.OnValueChanged += (_, newValue) => OnHealthChanged?.Invoke(newValue, kit.MaxHealth);
        StartCoroutine(InitializeHealthNextFrame());
    }

    private System.Collections.IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (this.HasServerAuthority())
            currentHealth.Value = kit.MaxHealth;
    }

    // Callable from any client; applied on the server.
    public void RequestDamage(float amount)
    {
        if (!NetworkObject.IsSpawned)
        {
            ApplyDamage(amount);
            return;
        }

        RequestDamageServerRpc(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageServerRpc(float amount)
    {
        ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        currentHealth.Value = Mathf.Max(0f, currentHealth.Value - amount);
        if (currentHealth.Value > 0f)
            return;

        OnEnemyKilled?.Invoke(kit.XpReward);

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
