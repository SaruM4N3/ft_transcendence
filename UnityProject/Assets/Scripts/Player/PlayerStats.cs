using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : NetworkBehaviour, IDamageable
{
    // Fallback used when no class stat set is resolved (e.g. offline test scenes).
    [SerializeField] private ClassStats defaultStats;
    [SerializeField] private PlayerCustomization customization;

    private ClassStats activeStats;
    private float maxHealth;
    private float maxMana;

    // Replicated so the lobby roster can show every player's health; mana stays purely local.
    private readonly NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float MaxHealth => maxHealth;
    public float MaxMana => maxMana;
    public float CurrentHealth => currentHealth.Value;
    public float CurrentMana { get; private set; }
    // Dead players stay dead within the scene - only a full scene reset (ResetToFull) revives them.
    public bool IsDead => currentHealth.Value <= 0f;

    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;

    // Fires for this instance specifically - lets the lobby roster bind to a non-local player's health.
    public event Action<float, float> OnHealthReplicated;

    void Awake()
    {
        if (customization == null)
            customization = GetComponent<PlayerCustomization>();

        RefreshActiveStats();
        CurrentMana = maxMana;
        currentHealth.OnValueChanged += (_, newValue) =>
        {
            OnHealthReplicated?.Invoke(newValue, maxHealth);
            if (this.IsLocallyControlled())
                OnHealthChanged?.Invoke(newValue, maxHealth);
        };
        if (customization != null)
            customization.OnClassOrColorChanged += (_, _) => HandleClassChanged();
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

    // Player NetworkObjects persist across scene loads (DestroyWithScene = false), so lobby PvP
    // damage would otherwise carry into the next match - wipe it clean on every scene arrival.
    public override void OnNetworkSpawn()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete += HandleSceneLoadComplete;
    }

    public override void OnNetworkDespawn()
    {
        // NetworkManager can already be null here when this fires as part of full shutdown teardown.
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete -= HandleSceneLoadComplete;
    }

    private void HandleSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (NetworkManager != null && clientId == NetworkManager.LocalClientId)
            // A real delay, not just a frame: RefreshActiveStats/maxHealth may not have re-resolved
            // the correct class yet, and Netcode's own post-load resync can otherwise clobber a
            // NetworkVariable write made too soon after the scene finishes loading.
            StartCoroutine(ResetToFullAfterSettling());
    }

    private System.Collections.IEnumerator ResetToFullAfterSettling()
    {
        yield return new WaitForSecondsRealtime(0.3f);
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

    // A class switch can lower maxHealth/maxMana - reclamp so current values never sit above the new max.
    private void HandleClassChanged()
    {
        RefreshActiveStats();
        CurrentMana = Mathf.Min(CurrentMana, maxMana);
        if (this.IsLocallyControlled())
            currentHealth.Value = Mathf.Min(currentHealth.Value, maxHealth);
    }

    // Pulls the current class's stat set from the customization menu's per-class table; falls back
    // to defaultStats when there's no customization component/menu (e.g. terrain test scenes).
    private void RefreshActiveStats()
    {
        ClassStats stats = null;
        if (customization != null && CharacterCustomizationMenu.Instance != null)
            stats = CharacterCustomizationMenu.Instance.GetStats(customization.ClassIndex);
        activeStats = stats != null ? stats : defaultStats;

        maxHealth = activeStats != null ? activeStats.MaxHealth : 100f;
        maxMana = activeStats != null ? activeStats.MaxMana : 50f;
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
