using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxMana = 50f;

    // Replicated so the lobby roster can show every player's health; mana stays purely local.
    private readonly NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float MaxHealth => maxHealth;
    public float MaxMana => maxMana;
    public float CurrentHealth => currentHealth.Value;
    public float CurrentMana { get; private set; }

    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;

    // Fires for this instance specifically - lets the lobby roster bind to a non-local player's health.
    public event Action<float, float> OnHealthReplicated;

    void Awake()
    {
        CurrentMana = maxMana;
        currentHealth.OnValueChanged += (_, newValue) =>
        {
            OnHealthReplicated?.Invoke(newValue, maxHealth);
            if (this.IsLocallyControlled())
                OnHealthChanged?.Invoke(newValue, maxHealth);
        };
        // Deferred a frame: writing a NetworkVariable directly from Awake() crashes IL2CPP/WebGL
        // builds ("indirect call to null" in NetworkVariable's generic-shared setter).
        StartCoroutine(InitializeHealthNextFrame());
    }

    private System.Collections.IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        currentHealth.Value = maxHealth;
    }

    void Start()
    {
        // Only the local/owned instance should drive this client's HUD.
        if (!this.IsLocallyControlled())
            return;

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnManaChanged?.Invoke(CurrentMana, maxMana);
    }

    // Avoids the warning Netcode logs when a non-owner writes an Owner-writable NetworkVariable.
    public void TakeDamage(float amount)
    {
        if (!this.IsLocallyControlled())
            return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0f, maxHealth);
    }

    // Callable from any client (e.g. an attacker's hit detection); routes to the target's own
    // owner so the Owner-permission currentHealth write above stays valid.
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

    public void Heal(float amount)
    {
        if (!this.IsLocallyControlled())
            return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0f, maxHealth);
    }

    public bool TrySpendMana(float amount)
    {
        if (CurrentMana < amount)
            return false;

        CurrentMana -= amount;
        if (this.IsLocallyControlled())
            OnManaChanged?.Invoke(CurrentMana, maxMana);
        return true;
    }

    public void RestoreMana(float amount)
    {
        CurrentMana = Mathf.Clamp(CurrentMana + amount, 0f, maxMana);
        if (this.IsLocallyControlled())
            OnManaChanged?.Invoke(CurrentMana, maxMana);
    }
}
