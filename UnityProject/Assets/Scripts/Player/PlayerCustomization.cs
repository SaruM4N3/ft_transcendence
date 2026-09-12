using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Replicates the local player's class/color/name to every client. Subscribed in Awake (not
// OnNetworkSpawn) so the same code path also drives the scene's offline, never-spawned player.
public class PlayerCustomization : NetworkBehaviour
{
    // Spawned instances only, used to detect/disambiguate duplicate names - see RefreshAllDisplayNames.
    private static readonly List<PlayerCustomization> ActiveInstances = new List<PlayerCustomization>();

    public static IReadOnlyList<PlayerCustomization> AllActiveInstances => ActiveInstances;

    // Lets the lobby roster add/remove a row per player instead of polling.
    public static event System.Action<PlayerCustomization> OnPlayerRegistered;
    public static event System.Action<PlayerCustomization> OnPlayerUnregistered;

    public event System.Action<int, int> OnClassOrColorChanged;
    public event System.Action<string> OnDisplayNameChanged;
    public event System.Action<int> OnTeamChanged;

    // Disambiguated name (e.g. "Bob (2)") - lets a newly bound roster row read it immediately.
    public string CurrentDisplayName { get; private set; }

    private readonly NetworkVariable<int> classIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> colorIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // Per-session/per-mode choice (which team to join at ready-check time) - not saved to PlayerProfileStore.
    private readonly NetworkVariable<int> teamIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Server-writable (not Owner) so ServerResetReady() can clear any player's readiness; SetReady() below
    // goes through a ServerRpc since even the server can't bypass the write-permission check directly.
    private readonly NetworkVariable<bool> isReady = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int ClassIndex => classIndex.Value;
    public int ColorIndex => colorIndex.Value;
    public string PlayerName => playerName.Value.ToString();
    public int TeamIndex => teamIndex.Value;
    public bool IsReady => isReady.Value;

    public event System.Action<bool> OnReadyChanged;

    [SerializeField] private PlayerNameTag nameTag;

    private void Awake()
    {
        classIndex.OnValueChanged += (_, _) => { ApplyVisuals(); OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value); };
        colorIndex.OnValueChanged += (_, _) => { ApplyVisuals(); OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value); };
        // A rename can create/resolve a collision with another player, so re-derive every name, not just this one.
        playerName.OnValueChanged += (_, _) => RefreshAllDisplayNames();
        teamIndex.OnValueChanged += (_, newValue) => OnTeamChanged?.Invoke(newValue);
        isReady.OnValueChanged += (_, newValue) => OnReadyChanged?.Invoke(newValue);
    }

    public override void OnNetworkSpawn()
    {
        ApplyVisuals();
        // OnValueChanged doesn't fire for the initial value a late-joining/observing client
        // receives, so any listener that read a not-yet-synced default in its own OnEnable
        // (e.g. MouseDirectionIndicator) needs this explicit nudge once spawn data has landed.
        OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value);
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

    // Duplicate raw names get " (1)"/" (2)" appended, ordered by NetworkObjectId so every client agrees.
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

    // Unlike OnNetworkSpawn (never fires offline), Start always runs - loads the save file so a
    // returning player keeps their class/color/name.
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

        // HUD's OnEnable sync already ran off the pre-load defaults - nudge it to pick up the loaded values.
        if (CharacterCustomizationMenu.Instance != null)
            CharacterCustomizationMenu.Instance.NotifyProfileLoaded(gameObject);
    }

    public void SetSelection(int newClassIndex, int newColorIndex)
    {
        if (!this.IsLocallyControlled())
            return;

        classIndex.Value = newClassIndex;
        colorIndex.Value = newColorIndex;
        PlayerProfileStore.Save(classIndex.Value, colorIndex.Value, playerName.Value.ToString());
    }

    public void SetTeam(int newTeamIndex)
    {
        if (!this.IsLocallyControlled())
            return;

        teamIndex.Value = newTeamIndex;
    }

    public void SetName(string newName)
    {
        if (!this.IsLocallyControlled())
            return;

        playerName.Value = newName;
        PlayerProfileStore.Save(classIndex.Value, colorIndex.Value, playerName.Value.ToString());
    }

    public void SetReady(bool ready)
    {
        if (!this.IsLocallyControlled())
            return;

        // Offline solo player has no RPC channel to send on.
        if (!NetworkObject.IsSpawned)
            return;

        SetReadyServerRpc(ready);
    }

    [ServerRpc]
    private void SetReadyServerRpc(bool ready)
    {
        isReady.Value = ready;
    }

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
