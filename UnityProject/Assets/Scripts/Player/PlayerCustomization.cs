using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>Replicates the local player's class/color/name choice (from CharacterCustomizationMenu) to
/// every connected client. Subscribed in Awake rather than OnNetworkSpawn so the same code path also
/// drives the scene's offline, never-spawned player instance (see IsLocallyControlled).</summary>
public class PlayerCustomization : NetworkBehaviour
{
    // Every spawned (networked) instance, used to detect and disambiguate duplicate names - see
    // RefreshAllDisplayNames. Never includes the offline, never-spawned player.
    private static readonly List<PlayerCustomization> ActiveInstances = new List<PlayerCustomization>();

    /// <summary>Read-only view of every currently spawned player - lets late-binding UI (e.g. the lobby
    /// roster) pick up players that registered before it started listening.</summary>
    public static IReadOnlyList<PlayerCustomization> AllActiveInstances => ActiveInstances;

    /// <summary>Fired when a player's NetworkObject spawns/despawns - lets the lobby roster add/remove
    /// a row per player instead of polling.</summary>
    public static event System.Action<PlayerCustomization> OnPlayerRegistered;
    public static event System.Action<PlayerCustomization> OnPlayerUnregistered;

    /// <summary>Fired whenever this player's class or color changes.</summary>
    public event System.Action<int, int> OnClassOrColorChanged;

    /// <summary>Fired with this player's already-disambiguated display name (see
    /// RefreshAllDisplayNames) whenever it changes.</summary>
    public event System.Action<string> OnDisplayNameChanged;

    /// <summary>The last computed disambiguated name (e.g. "Bob (2)") - lets a newly bound roster row
    /// read the current value immediately instead of waiting for the next OnDisplayNameChanged.</summary>
    public string CurrentDisplayName { get; private set; }

    private readonly NetworkVariable<int> classIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> colorIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Server-writable, not Owner like the others above: ServerResetReady() needs to clear every
    // player's readiness, including remote clients who own their own instance. A NetworkVariable's
    // write permission is enforced even for a direct server-side .Value set, so SetReady() below goes
    // through a ServerRpc instead of writing directly.
    private readonly NetworkVariable<bool> isReady = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int ClassIndex => classIndex.Value;
    public int ColorIndex => colorIndex.Value;
    public string PlayerName => playerName.Value.ToString();
    public bool IsReady => isReady.Value;

    /// <summary>Fired whenever this player's ready-check state changes - the ready check UI binds to a
    /// specific player's row through this.</summary>
    public event System.Action<bool> OnReadyChanged;

    [SerializeField] private PlayerNameTag nameTag;

    private void Awake()
    {
        classIndex.OnValueChanged += (_, _) => { ApplyVisuals(); OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value); };
        colorIndex.OnValueChanged += (_, _) => { ApplyVisuals(); OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value); };
        // A rename can create or resolve a collision with another player too, not just this one, so
        // any change re-derives every spawned player's displayed name rather than just this one's.
        playerName.OnValueChanged += (_, _) => RefreshAllDisplayNames();
        isReady.OnValueChanged += (_, newValue) => OnReadyChanged?.Invoke(newValue);
    }

    public override void OnNetworkSpawn()
    {
        ApplyVisuals();
        ActiveInstances.Add(this);
        RefreshAllDisplayNames();
        OnPlayerRegistered?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        ActiveInstances.Remove(this);
        OnPlayerUnregistered?.Invoke(this);
        RefreshAllDisplayNames();
    }

    // Two players who picked the same raw name get " (1)"/" (2)"/... appended, ordered by
    // NetworkObjectId so every client independently computes the same result - there's no
    // server-authoritative "display name".
    private static void RefreshAllDisplayNames()
    {
        var groups = new Dictionary<string, List<PlayerCustomization>>();
        foreach (PlayerCustomization instance in ActiveInstances)
        {
            string baseName = instance.playerName.Value.ToString();
            if (!groups.TryGetValue(baseName, out List<PlayerCustomization> group))
            {
                group = new List<PlayerCustomization>();
                groups[baseName] = group;
            }
            group.Add(instance);
        }

        foreach (List<PlayerCustomization> group in groups.Values)
        {
            group.Sort((a, b) => a.NetworkObjectId.CompareTo(b.NetworkObjectId));
            for (int i = 0; i < group.Count; i++)
            {
                string baseName = group[i].playerName.Value.ToString();
                group[i].ApplyName(group.Count > 1 ? $"{baseName} ({i + 1})" : baseName);
            }
        }
    }

    // Runs for every instance regardless of Netcode state, unlike OnNetworkSpawn which never fires
    // offline - see IsLocallyControlled. Loads the local save file so a returning player keeps their
    // class/color/name without reopening the menu.
    private void Start()
    {
        if (!this.IsLocallyControlled())
            return;

        if (!PlayerProfileStore.TryLoad(out int savedClassIndex, out int savedColorIndex, out string savedName))
            return;

        classIndex.Value = savedClassIndex;
        colorIndex.Value = savedColorIndex;
        if (!string.IsNullOrEmpty(savedName))
            playerName.Value = savedName;

        // The HUD already ran its one-time initial sync (in OnEnable, which fires before this Start)
        // off the pre-load defaults, so nudge it to pick up what we just loaded.
        if (CharacterCustomizationMenu.Instance != null)
            CharacterCustomizationMenu.Instance.NotifyProfileLoaded(gameObject);
    }

    /// <summary>Called by CharacterCustomizationMenu when this player picks a class/color. Ignored on a
    /// spawned instance that isn't locally owned.</summary>
    public void SetSelection(int newClassIndex, int newColorIndex)
    {
        if (!this.IsLocallyControlled())
            return;

        classIndex.Value = newClassIndex;
        colorIndex.Value = newColorIndex;
        PlayerProfileStore.Save(classIndex.Value, colorIndex.Value, playerName.Value.ToString());
    }

    /// <summary>Called by CharacterCustomizationMenu when this player sets their display name. Same
    /// owner-only restriction as SetSelection.</summary>
    public void SetName(string newName)
    {
        if (!this.IsLocallyControlled())
            return;

        playerName.Value = newName;
        PlayerProfileStore.Save(classIndex.Value, colorIndex.Value, playerName.Value.ToString());
    }

    /// <summary>Called by the ready-check UI when this (local) player toggles their own readiness.
    /// Goes through a ServerRpc rather than writing isReady directly, since it's Server-writable, not
    /// Owner-writable - see isReady.</summary>
    public void SetReady(bool ready)
    {
        if (!this.IsLocallyControlled())
            return;

        // The ready check only exists in a networked session - the offline solo player has no RPC
        // channel to send on.
        if (!NetworkObject.IsSpawned)
            return;

        SetReadyServerRpc(ready);
    }

    // Default RequireOwnership (true) is what's wanted here, unlike ModeReadyCheck's own RPC: this one
    // lives on a specific player's own NetworkObject, so only that player's owning client can invoke it.
    [ServerRpc]
    private void SetReadyServerRpc(bool ready)
    {
        isReady.Value = ready;
    }

    /// <summary>Called by ModeReadyCheck (server-only) to clear readiness for every player when a new
    /// ready check starts.</summary>
    public void ServerResetReady()
    {
        if (!IsServer)
            return;

        isReady.Value = false;
    }

    private void ApplyVisuals()
    {
        if (CharacterCustomizationMenu.Instance != null)
            CharacterCustomizationMenu.Instance.ApplyVisuals(gameObject, classIndex.Value, colorIndex.Value);
    }

    private void ApplyName(string value)
    {
        CurrentDisplayName = value;
        if (nameTag != null)
            nameTag.SetName(value);
        OnDisplayNameChanged?.Invoke(value);
    }
}
