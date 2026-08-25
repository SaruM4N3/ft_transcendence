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
    private readonly NetworkVariable<int> classIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> colorIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public int ClassIndex => classIndex.Value;
    public int ColorIndex => colorIndex.Value;
    public string PlayerName => playerName.Value.ToString();

    [SerializeField] private PlayerNameTag nameTag;

    private void Awake()
    {
        classIndex.OnValueChanged += (_, _) => ApplyVisuals();
        colorIndex.OnValueChanged += (_, _) => ApplyVisuals();
        playerName.OnValueChanged += (_, newValue) => ApplyName(newValue);
    }

    // A late-joining client's copy of an already-customized player needs the current values applied
    // once on spawn, since OnValueChanged only fires for changes from here on.
    public override void OnNetworkSpawn()
    {
        ApplyVisuals();
        ApplyName(playerName.Value);
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

    private void ApplyVisuals()
    {
        if (CharacterCustomizationMenu.Instance != null)
            CharacterCustomizationMenu.Instance.ApplyVisuals(gameObject, classIndex.Value, colorIndex.Value);
    }

    private void ApplyName(FixedString32Bytes value)
    {
        if (nameTag != null)
            nameTag.SetName(value.ToString());
    }
}
