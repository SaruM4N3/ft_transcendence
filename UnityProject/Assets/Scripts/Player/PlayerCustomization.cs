using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>Replicates the local player's class/color/name choice (made through
/// CharacterCustomizationMenu) to every connected client, so a change is visible on this player's
/// networked puppet everywhere - not just on the owner's own screen. Subscribed in Awake rather than
/// OnNetworkSpawn so the same code path also drives the scene's offline, never-spawned player instance
/// (see NetworkBehaviourExtensions.IsLocallyControlled).</summary>
public class PlayerCustomization : NetworkBehaviour
{
    // Every spawned (networked) instance, used to detect and disambiguate duplicate names - see
    // RefreshAllDisplayNames. Never includes the offline, never-spawned player.
    private static readonly List<PlayerCustomization> ActiveInstances = new List<PlayerCustomization>();

    /// <summary>Read-only view of every currently spawned player - lets the lobby roster UI pick up
    /// players that registered before it started listening (e.g. it enables after they've already
    /// spawned), in addition to the OnPlayerRegistered/OnPlayerUnregistered events below.</summary>
    public static IReadOnlyList<PlayerCustomization> AllActiveInstances => ActiveInstances;

    /// <summary>Fired once a player's NetworkObject has spawned (and its raw state is already synced -
    /// see OnNetworkSpawn) / right before it despawns. The lobby roster UI uses these to add/remove a
    /// row per player instead of polling.</summary>
    public static event System.Action<PlayerCustomization> OnPlayerRegistered;
    public static event System.Action<PlayerCustomization> OnPlayerUnregistered;

    /// <summary>Fired whenever this player's class or color changes, for anything (like a roster row)
    /// that needs to react to a specific player's visuals without going through the local-only
    /// CharacterCustomizationMenu.OnPortraitChanged static event.</summary>
    public event System.Action<int, int> OnClassOrColorChanged;

    /// <summary>Fired with this player's already-disambiguated display name (see
    /// RefreshAllDisplayNames) whenever it changes - mirrors what's shown on the world nametag, for
    /// anything else (like a roster row) that needs the same text.</summary>
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

    // Mode-select ready check (see ModeReadyCheck) - Server-writable, not Owner like the others above.
    // A NetworkVariable's write permission is enforced even for a direct server-side .Value set when
    // the declared writer isn't the server - ServerResetReady() needs to clear every player's
    // readiness (including remote clients who own their own instance), so the server has to be the
    // actual writer here; SetReady() below goes through a ServerRpc rather than writing directly.
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

    // Two players who picked the same raw name get " (1)"/" (2)"/... appended to what's shown above
    // their heads, ordered by NetworkObjectId so every client - there's no server-authoritative
    // "display name" - independently computes the exact same result from state it already has (raw
    // names are NetworkVariableReadPermission.Everyone, so every client already sees every player's
    // chosen name).
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

    // Runs for every instance (offline scene-placed or networked spawn) regardless of Netcode state,
    // unlike OnNetworkSpawn which never fires offline - see IsLocallyControlled. Loads the local save
    // file and applies it through the same owner-only setters the customization menu uses, so a
    // returning player keeps their class/color/name without having to reopen the menu.
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
    /// spawned instance that isn't locally owned - only the real owner is allowed to write these
    /// NetworkVariables.</summary>
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
    /// Same owner-only restriction as SetSelection/SetName, but goes through a ServerRpc rather than
    /// writing isReady directly, since it's Server-writable, not Owner-writable - see isReady.</summary>
    public void SetReady(bool ready)
    {
        if (!this.IsLocallyControlled())
            return;

        // The ready check only ever exists in a networked session - if this instance somehow isn't
        // spawned (e.g. called on the offline solo player), there's no RPC channel to send on.
        if (!NetworkObject.IsSpawned)
            return;

        SetReadyServerRpc(ready);
    }

    // Default RequireOwnership (true) is exactly what's wanted here, unlike ModeReadyCheck's own RPC -
    // this one lives on a specific player's own NetworkObject, so only that player's owning client is
    // allowed to invoke it, which already prevents one player from toggling another's readiness.
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
