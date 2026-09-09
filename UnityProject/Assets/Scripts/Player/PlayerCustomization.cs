using Unity.Netcode;
using UnityEngine;

/// <summary>Replicates the local player's class/color choice (made through
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

    public int ClassIndex => classIndex.Value;
    public int ColorIndex => colorIndex.Value;

    private void Awake()
    {
        classIndex.OnValueChanged += (_, _) => ApplyVisuals();
        colorIndex.OnValueChanged += (_, _) => ApplyVisuals();
    }

    // A late-joining client's copy of an already-customized player needs the current values applied
    // once on spawn, since OnValueChanged only fires for changes from here on.
    public override void OnNetworkSpawn()
    {
        ApplyVisuals();
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
    }

    private void ApplyVisuals()
    {
        if (CharacterCustomizationMenu.Instance != null)
            CharacterCustomizationMenu.Instance.ApplyVisuals(gameObject, classIndex.Value, colorIndex.Value);
    }
}
