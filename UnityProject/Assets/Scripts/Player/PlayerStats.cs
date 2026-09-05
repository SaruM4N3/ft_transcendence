using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : NetworkBehaviour, IDamageable
{
    [SerializeField] private ClassStats defaultStats;
    [SerializeField] private PlayerCustomization customization;

    private ClassStats activeStats;
    private float maxHealth;
    private float maxMana;

    private readonly NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float MaxHealth => maxHealth;
    public float MaxMana => maxMana;
    public float CurrentHealth => currentHealth.Value;
    public float CurrentMana { get; private set; }
    public bool IsDead => currentHealth.Value <= 0f;

    public static bool DeathProtected { get; set; }

    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;
    public static event Action<PlayerStats> OnPlayerDied;

    public event Action<float, float> OnHealthReplicated;

    void Awake()
    {
        if (customization == null)
            customization = GetComponent<PlayerCustomization>();

        RefreshActiveStats();
        CurrentMana = maxMana;
        currentHealth.OnValueChanged += (previousValue, newValue) =>
        {
            if (previousValue > 0f && newValue <= 0f)
                OnPlayerDied?.Invoke(this);
            OnHealthReplicated?.Invoke(newValue, maxHealth);
            if (this.IsLocallyControlled())
                OnHealthChanged?.Invoke(newValue, maxHealth);
        };
        if (customization != null)
            customization.OnClassOrColorChanged += (_, _) => HandleClassChanged();
        StartCoroutine(InitializeHealthNextFrame());
    }

    private System.Collections.IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (this.IsLocallyControlled())
            currentHealth.Value = maxHealth;
    }

    void Start()
    {
        if (!this.IsLocallyControlled())
            return;

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnManaChanged?.Invoke(CurrentMana, maxMana);
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadEventCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadEventCompleted;
    }

    private void HandleSceneLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        ResetToFull();
    }

    public void ResetToFull()
    {
        if (!this.IsLocallyControlled())
            return;

        currentHealth.Value = maxHealth;
        CurrentMana = maxMana;
        OnManaChanged?.Invoke(CurrentMana, maxMana);
    }

    void Update()
    {
        if (!this.IsLocallyControlled() || activeStats == null || IsDead)
            return;

        if (CurrentHealth < maxHealth)
            Heal(activeStats.HealthRegenPerSecond * Time.deltaTime);
        if (CurrentMana < maxMana)
            RestoreMana(activeStats.ManaRegenPerSecond * Time.deltaTime);
    }

    private void HandleClassChanged()
    {
        RefreshActiveStats();
        CurrentMana = Mathf.Min(CurrentMana, maxMana);
        if (this.IsLocallyControlled())
            currentHealth.Value = Mathf.Min(currentHealth.Value, maxHealth);
    }

    private void RefreshActiveStats()
    {
        ClassStats stats = null;
        if (customization != null && CharacterCustomizationMenu.Instance != null)
            stats = CharacterCustomizationMenu.Instance.GetStats(customization.ClassIndex);
        activeStats = stats != null ? stats : defaultStats;

        maxHealth = activeStats != null ? activeStats.MaxHealth : 100f;
        maxMana = activeStats != null ? activeStats.MaxMana : 50f;
    }

    public void TakeDamage(float amount)
    {
        if (!this.IsLocallyControlled())
            return;

        float minHealth = DeathProtected ? 1f : 0f;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, Mathf.Min(minHealth, currentHealth.Value), maxHealth);
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
