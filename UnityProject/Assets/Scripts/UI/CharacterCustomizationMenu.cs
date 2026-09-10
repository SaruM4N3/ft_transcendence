using TMPro;
using UnityEngine;

public class CharacterCustomizationMenu : MonoBehaviour
{
    /// <summary>Lets PlayerCustomization (on any networked player instance) reapply visuals without
    /// needing its own copy of the classPresets/colorVariants lookup tables. Falls back to an
    /// inactive-inclusive scene search because this menu's panel GameObject starts disabled - Awake()
    /// (which normally sets this) doesn't run until the panel is opened for the first time, which must
    /// not be a prerequisite for a remote player's customization to apply.</summary>
    public static CharacterCustomizationMenu Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<CharacterCustomizationMenu>(FindObjectsInactive.Include);
            return instance;
        }
    }
    private static CharacterCustomizationMenu instance;

    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private int maxNameLength = 20;
    /// <summary>A non-networked, gameplay-stripped character instance rendered by a dedicated
    /// preview camera (see PreviewStage in the Lobby scene) onto the panel's Preview RawImage - kept
    /// in sync with the local player's current class/color selection.</summary>
    [SerializeField] private GameObject previewCharacter;

    private void Awake()
    {
        instance = this;

        if (nameInputField != null)
            nameInputField.onEndEdit.AddListener(SetPlayerName);
    }

    // The panel is reopened every time an NPC menu is triggered (Open() just re-activates it), so
    // refresh the field from the player's current name each time rather than only once in Awake.
    private void OnEnable()
    {
        GameObject player = GetLocalPlayer();
        if (player == null)
            return;

        if (nameInputField != null)
        {
            PlayerCustomization customization = player.GetComponent<PlayerCustomization>();
            if (customization != null)
                nameInputField.text = customization.PlayerName;
        }

        if (previewCharacter != null)
            ApplyVisuals(previewCharacter, CurrentClassIndex(player), CurrentColorIndex(player));
    }

    /// <summary>Wired to the name input field's OnEndEdit - applies and replicates the local player's
    /// chosen display name, mirroring SelectClass/SelectColor's Apply() flow.</summary>
    public void SetPlayerName(string value)
    {
        value = value.Trim();
        if (string.IsNullOrEmpty(value))
            return;
        if (value.Length > maxNameLength)
            value = value.Substring(0, maxNameLength);

        GameObject player = GetLocalPlayer();
        if (player == null)
            return;

        PlayerCustomization customization = player.GetComponent<PlayerCustomization>();
        if (customization != null)
            customization.SetName(value);

        if (nameInputField != null && nameInputField.text != value)
            nameInputField.text = value;

        OnNameChanged?.Invoke(value);
    }

    /// <summary>With multiple networked players, GameObject.FindWithTag("Player") is ambiguous -
    /// this always resolves to the local client's own player, falling back to the tag lookup only
    /// when there's no active network session (e.g. testing in the editor without hosting/joining).</summary>
    private GameObject GetLocalPlayer()
    {
        Unity.Netcode.NetworkManager nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm != null && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
            return nm.LocalClient.PlayerObject.gameObject;

        return GameObject.FindWithTag("Player");
    }

    public static event System.Action<Sprite> OnPortraitChanged;
    public static event System.Action<Sprite> OnBackgroundChanged;
    public static event System.Action<string> OnNameChanged;

    [SerializeField] private GameObject[] classPresets;
    [SerializeField] private ColorVariant[] colorVariants;
    /// <summary>HUD background sword sprite per color (not per class) - indexed the same as colorVariants.</summary>
    [SerializeField] private Sprite[] backgroundSpritesByColor;

    /// <summary>One color's controller+sprites for every class, indexed the same as classPresets.</summary>
    [System.Serializable]
    private class ColorVariant
    {
        public RuntimeAnimatorController[] controllersByClass;
        /// <summary>World character sprite (the class's own Idle frame) - shown on the player in the scene.</summary>
        public Sprite[] worldSpritesByClass;
        /// <summary>HUD bust icon (Human Avatars) - shown in the top-left portrait, never on the world character.</summary>
        public Sprite[] portraitSpritesByClass;
    }

    /// <summary>Resolves the portrait matching the player's current class+color, for the HUD to sync to
    /// on startup even if this menu has never been opened (so its own Awake/OnEnable haven't run).</summary>
    public Sprite GetCurrentPortrait()
    {
        GameObject player = GetLocalPlayer();
        if (player == null)
            return null;

        int classIndex = CurrentClassIndex(player);
        int colorIndex = CurrentColorIndex(player);
        if (classIndex < 0 || classIndex >= classPresets.Length)
            return null;
        if (colorIndex < 0 || colorIndex >= colorVariants.Length)
            return null;

        return colorVariants[colorIndex].portraitSpritesByClass[classIndex];
    }

    /// <summary>Resolves the HUD background sword sprite matching the player's current color, for the
    /// same startup-sync reason as GetCurrentPortrait.</summary>
    public Sprite GetCurrentBackground()
    {
        GameObject player = GetLocalPlayer();
        if (player == null)
            return null;

        int colorIndex = CurrentColorIndex(player);
        if (colorIndex < 0 || colorIndex >= backgroundSpritesByColor.Length)
            return null;

        return backgroundSpritesByColor[colorIndex];
    }

    /// <summary>Resolves the local player's current display name, for the HUD to sync to on startup
    /// even if this menu has never been opened - same startup-sync reason as GetCurrentPortrait.</summary>
    public string GetCurrentName()
    {
        GameObject player = GetLocalPlayer();
        if (player == null)
            return string.Empty;

        PlayerCustomization customization = player.GetComponent<PlayerCustomization>();
        return customization != null ? customization.PlayerName : string.Empty;
    }

    public void SelectClass(int index)
    {
        if (index < 0 || index >= classPresets.Length || classPresets[index] == null)
            return;

        GameObject player = GetLocalPlayer();
        if (player == null)
            return;

        Apply(player, index, CurrentColorIndex(player));
    }

    public void SelectColor(int index)
    {
        if (index < 0 || index >= colorVariants.Length)
            return;

        GameObject player = GetLocalPlayer();
        if (player == null)
            return;

        Apply(player, CurrentClassIndex(player), index);
    }

    private void Apply(GameObject player, int classIndex, int colorIndex)
    {
        if (!ApplyVisuals(player, classIndex, colorIndex))
            return;

        if (previewCharacter != null)
            ApplyVisuals(previewCharacter, classIndex, colorIndex);

        OnPortraitChanged?.Invoke(colorVariants[colorIndex].portraitSpritesByClass[classIndex]);
        if (colorIndex < backgroundSpritesByColor.Length)
            OnBackgroundChanged?.Invoke(backgroundSpritesByColor[colorIndex]);

        // Replicate the choice to every other connected client - Apply() only ever runs for the local
        // player (see GetLocalPlayer()), so this is always the real owner making the change.
        PlayerCustomization customization = player.GetComponent<PlayerCustomization>();
        if (customization != null)
            customization.SetSelection(classIndex, colorIndex);
    }

    /// <summary>Re-broadcasts the local player's current portrait/background/name to the HUD. Needed
    /// when PlayerCustomization loads a saved profile in its own Start() - that happens after the
    /// HUD's OnEnable-time initial sync (GetCurrentPortrait/Background/Name) and doesn't go through
    /// Apply()/SetPlayerName(), so without this the HUD would keep showing stale defaults until the
    /// player touched the menu again even though the world sprite/nametag updated correctly.</summary>
    public void NotifyProfileLoaded(GameObject player)
    {
        int classIndex = CurrentClassIndex(player);
        int colorIndex = CurrentColorIndex(player);
        if (classIndex < 0 || classIndex >= classPresets.Length)
            return;
        if (colorIndex < 0 || colorIndex >= colorVariants.Length)
            return;

        OnPortraitChanged?.Invoke(colorVariants[colorIndex].portraitSpritesByClass[classIndex]);
        if (colorIndex < backgroundSpritesByColor.Length)
            OnBackgroundChanged?.Invoke(backgroundSpritesByColor[colorIndex]);

        PlayerCustomization customization = player.GetComponent<PlayerCustomization>();
        if (customization != null && !string.IsNullOrEmpty(customization.PlayerName))
            OnNameChanged?.Invoke(customization.PlayerName);
    }

    /// <summary>The mechanical half of a customization change (Animator controller + world sprite),
    /// with no HUD side effects. Called from Apply() for the local player, and by PlayerCustomization to
    /// reapply a networked puppet's replicated class/color - which must never touch the local HUD's
    /// portrait/background, since that puppet usually isn't the local player.</summary>
    public bool ApplyVisuals(GameObject player, int classIndex, int colorIndex)
    {
        if (classIndex < 0 || classIndex >= classPresets.Length)
            return false;
        if (colorIndex < 0 || colorIndex >= colorVariants.Length)
            return false;

        ColorVariant variant = colorVariants[colorIndex];
        RuntimeAnimatorController controller = variant.controllersByClass[classIndex];
        Sprite worldSprite = variant.worldSpritesByClass[classIndex];
        if (controller == null)
            return false;

        // Assigning the controller re-applies the animator's current (old) frame synchronously,
        // so the sprite must be set last or it gets clobbered.
        player.GetComponent<Animator>().runtimeAnimatorController = controller;
        player.GetComponent<SpriteRenderer>().sprite = worldSprite;
        return true;
    }

    /// <summary>Figures out which class preset the player's current controller belongs to, by
    /// comparing against the un-overridden base controller (its own, or an override's base) - so
    /// picking a color never has to guess and silently reset the class to index 0.</summary>
    private int CurrentClassIndex(GameObject player)
    {
        RuntimeAnimatorController current = player.GetComponent<Animator>().runtimeAnimatorController;
        AnimatorOverrideController overrideController = current as AnimatorOverrideController;
        RuntimeAnimatorController baseController = overrideController != null ? overrideController.runtimeAnimatorController : current;

        for (int i = 0; i < classPresets.Length; i++)
        {
            if (classPresets[i].GetComponent<Animator>().runtimeAnimatorController == baseController)
                return i;
        }

        return 0;
    }

    /// <summary>Figures out which color variant the player's current controller belongs to.</summary>
    private int CurrentColorIndex(GameObject player)
    {
        RuntimeAnimatorController current = player.GetComponent<Animator>().runtimeAnimatorController;

        for (int c = 0; c < colorVariants.Length; c++)
        {
            foreach (RuntimeAnimatorController candidate in colorVariants[c].controllersByClass)
            {
                if (candidate == current)
                    return c;
            }
        }

        return 0;
    }
}
