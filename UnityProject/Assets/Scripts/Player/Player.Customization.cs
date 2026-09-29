using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public partial class Player
{
    private static readonly List<Player> ActiveInstances = new List<Player>();

    public static IReadOnlyList<Player> AllActiveInstances => ActiveInstances;

    public static event Action<Player> OnPlayerRegistered;
    public static event Action<Player> OnPlayerUnregistered;

    public event Action<int, int> OnClassOrColorChanged;
    public event Action<string> OnDisplayNameChanged;
    public event Action<int> OnTeamChanged;
    public event Action<bool> OnReadyChanged;

    public string CurrentDisplayName { get; private set; }

    private readonly NetworkVariable<int> classIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> colorIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> teamIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Server-writable so readiness can be reset; SetReady goes through a ServerRpc.
    private readonly NetworkVariable<bool> isReady = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int ClassIndex => classIndex.Value;
    public int ColorIndex => colorIndex.Value;
    public string PlayerName => playerName.Value.ToString();
    public int TeamIndex => teamIndex.Value;
    public bool IsReady => isReady.Value;

    [Header("Customization")]
    [SerializeField] private PlayerNameTag nameTag;
    [SerializeField] private Camera previewCamera;

    private int layerBeforePreview;

    // Toggled while the customization panel is open; also swaps to the CharacterPreview layer so its camera sees only this player.
    public void SetPreviewCameraActive(bool active)
    {
        if (previewCamera != null)
            previewCamera.gameObject.SetActive(active);

        if (active)
        {
            layerBeforePreview = gameObject.layer;
            gameObject.layer = LayerMask.NameToLayer("CharacterPreview");
        }
        else
        {
            gameObject.layer = layerBeforePreview;
        }
    }

    private void AwakeCustomization()
    {
        classIndex.OnValueChanged += (_, _) => { ApplyVisuals(); OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value); };
        colorIndex.OnValueChanged += (_, _) => { ApplyVisuals(); OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value); };
        playerName.OnValueChanged += (_, _) => RefreshAllDisplayNames();
        teamIndex.OnValueChanged += (_, newValue) => OnTeamChanged?.Invoke(newValue);
        isReady.OnValueChanged += (_, newValue) => OnReadyChanged?.Invoke(newValue);
    }

    private void SpawnCustomization()
    {
        ApplyVisuals();
        OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value);
        ActiveInstances.Add(this);
        RefreshAllDisplayNames();
        OnPlayerRegistered?.Invoke(this);
    }

    private void DespawnCustomization()
    {
        ActiveInstances.Remove(this);
        OnPlayerUnregistered?.Invoke(this);
        RefreshAllDisplayNames();
    }

    private static void RefreshAllDisplayNames()
    {
        var groups = new Dictionary<string, List<Player>>();
        foreach (Player instance in ActiveInstances)
        {
            string baseName = instance.playerName.Value.ToString();
            if (!groups.TryGetValue(baseName, out List<Player> group))
            {
                group = new List<Player>();
                groups[baseName] = group;
            }
            group.Add(instance);
        }

        foreach (List<Player> group in groups.Values)
        {
            group.Sort((a, b) => a.NetworkObjectId.CompareTo(b.NetworkObjectId));
            for (int i = 0; i < group.Count; i++)
            {
                string baseName = group[i].playerName.Value.ToString();
                group[i].ApplyName(group.Count > 1 ? $"{baseName} ({i + 1})" : baseName);
            }
        }
    }

    private void StartCustomization()
    {
        if (!this.IsLocallyControlled())
            return;

        if (!PlayerProfileStore.TryLoad(out int savedClassIndex, out int savedColorIndex, out string savedName))
            return;

        classIndex.Value = savedClassIndex;
        colorIndex.Value = savedColorIndex;
        if (!string.IsNullOrEmpty(savedName))
            playerName.Value = savedName;

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
