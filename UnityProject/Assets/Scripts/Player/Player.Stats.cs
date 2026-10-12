using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class Player
{
    private float maxHealth;
    private float maxMana;

    private readonly NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float MaxHealth => maxHealth + LevelUpHealthBonus;
    public float MaxMana => maxMana + LevelUpManaBonus;
    public float CurrentHealth => currentHealth.Value;
    public float CurrentMana { get; private set; }
    public bool IsDead => currentHealth.Value <= 0f;

    public static bool DeathProtected { get; set; }

    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;
    public static event Action<Player> OnPlayerDied;

    public event Action<float, float> OnHealthReplicated;

    private void AwakeStats()
    {
        RefreshActiveKit();
        RefreshStatsFromKit();
        CurrentMana = MaxMana;
        currentHealth.OnValueChanged += (previousValue, newValue) =>
        {
            if (previousValue > 0f && newValue <= 0f)
                OnPlayerDied?.Invoke(this);
            OnHealthReplicated?.Invoke(newValue, MaxHealth);
            if (this.IsLocallyControlled())
                OnHealthChanged?.Invoke(newValue, MaxHealth);
        };
        OnClassOrColorChanged += (_, _) => HandleClassChanged();
        StartCoroutine(InitializeHealthNextFrame());
    }

    private IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (this.IsLocallyControlled())
            currentHealth.Value = MaxHealth;
    }

    private void StartStats()
    {
        if (!this.IsLocallyControlled())
            return;

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    private void SpawnStats()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadEventCompleted;
    }

    private void DespawnStats()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadEventCompleted;
    }

    private void HandleSceneLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        ResetToFull();
    }

    public void ResetToFull()
    {
        if (!this.IsLocallyControlled())
            return;

        currentHealth.Value = MaxHealth;
        CurrentMana = MaxMana;
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    private void UpdateStats()
    {
        if (!this.IsLocallyControlled() || activeKit == null || IsDead)
            return;

        if (CurrentHealth < MaxHealth)
            Heal((activeKit.HealthRegenPerSecond + LevelUpHealthRegenBonus) * Time.deltaTime);
        if (CurrentMana < MaxMana)
            RestoreMana((activeKit.ManaRegenPerSecond + LevelUpManaRegenBonus) * Time.deltaTime);
    }

    private void HandleClassChanged()
    {
        RefreshActiveKit();
        RefreshStatsFromKit();
        CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        if (this.IsLocallyControlled())
            currentHealth.Value = Mathf.Min(currentHealth.Value, MaxHealth);
    }

    private void RefreshStatsFromKit()
    {
        maxHealth = activeKit != null ? activeKit.MaxHealth : 100f;
        maxMana = activeKit != null ? activeKit.MaxMana : 50f;
    }

    public void TakeDamage(float amount)
    {
        if (!this.IsLocallyControlled())
            return;

        float minHealth = DeathProtected ? 1f : 0f;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, Mathf.Min(minHealth, currentHealth.Value), MaxHealth);
    }

    public void RequestDamage(float amount)
    {
        if (!NetworkObject.IsSpawned)
        {
            TakeDamage(amount);
            return;
        }
        RequestDamageServerRpc(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageServerRpc(float amount)
    {
        ApplyDamageClientRpc(amount, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void ApplyDamageClientRpc(float amount, ClientRpcParams rpcParams = default)
    {
        TakeDamage(amount);
    }

    // Called by a rider once the 20s mount channel completes; routed to the downed player's own owner, same pattern as damage.
    public void RequestRevive()
    {
        if (!NetworkObject.IsSpawned)
        {
            Revive();
            return;
        }
        RequestReviveServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestReviveServerRpc()
    {
        ApplyReviveClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void ApplyReviveClientRpc(ClientRpcParams rpcParams = default)
    {
        Revive();
    }

    private void Revive()
    {
        if (!this.IsLocallyControlled())
            return;

        currentHealth.Value = MaxHealth;
    }

    public void Heal(float amount)
    {
        if (!this.IsLocallyControlled())
            return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0f, MaxHealth);
    }

    public bool TrySpendMana(float amount)
    {
        if (CurrentMana < amount)
            return false;

        CurrentMana -= amount;
        if (this.IsLocallyControlled())
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
        return true;
    }

    public void RestoreMana(float amount)
    {
        CurrentMana = Mathf.Clamp(CurrentMana + amount, 0f, MaxMana);
        if (this.IsLocallyControlled())
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }
}
