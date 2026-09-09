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

    private void Awake()
    {
        instance = this;
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

        OnPortraitChanged?.Invoke(colorVariants[colorIndex].portraitSpritesByClass[classIndex]);
        if (colorIndex < backgroundSpritesByColor.Length)
            OnBackgroundChanged?.Invoke(backgroundSpritesByColor[colorIndex]);

        // Replicate the choice to every other connected client - Apply() only ever runs for the local
        // player (see GetLocalPlayer()), so this is always the real owner making the change.
        PlayerCustomization customization = player.GetComponent<PlayerCustomization>();
        if (customization != null)
            customization.SetSelection(classIndex, colorIndex);
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
