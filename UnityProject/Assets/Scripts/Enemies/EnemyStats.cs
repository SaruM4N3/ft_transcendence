using System;
using Unity.Netcode;
using UnityEngine;

// Networked health for a wave enemy. Unlike PlayerStats, the "owner" is always the server so the server can write currentHealth directly.
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
        StartCoroutine(InitializeHealthNextFrame());
    }

    private System.Collections.IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (this.HasServerAuthority())
            currentHealth.Value = maxHealth;
    }

    // Callable from any client (whichever attacker's hitbox landed the hit).
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

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
