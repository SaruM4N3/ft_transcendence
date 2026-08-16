using UnityEngine;

public class CharacterCustomizationMenu : MonoBehaviour
{
    public static event System.Action<Sprite> OnPortraitChanged;

    [SerializeField] private GameObject[] classPresets;
    [SerializeField] private ColorVariant[] colorVariants;

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
        GameObject player = GameObject.FindWithTag("Player");
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

    public void SelectClass(int index)
    {
        if (index < 0 || index >= classPresets.Length || classPresets[index] == null)
            return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
            return;

        Apply(player, index, CurrentColorIndex(player));
    }

    public void SelectColor(int index)
    {
        if (index < 0 || index >= colorVariants.Length)
            return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
            return;

        Apply(player, CurrentClassIndex(player), index);
    }

    private void Apply(GameObject player, int classIndex, int colorIndex)
    {
        if (classIndex < 0 || classIndex >= classPresets.Length)
            return;
        if (colorIndex < 0 || colorIndex >= colorVariants.Length)
            return;

        ColorVariant variant = colorVariants[colorIndex];
        RuntimeAnimatorController controller = variant.controllersByClass[classIndex];
        Sprite worldSprite = variant.worldSpritesByClass[classIndex];
        if (controller == null)
            return;

        // Assigning the controller re-applies the animator's current (old) frame synchronously,
        // so the sprite must be set last or it gets clobbered.
        player.GetComponent<Animator>().runtimeAnimatorController = controller;
        player.GetComponent<SpriteRenderer>().sprite = worldSprite;
        OnPortraitChanged?.Invoke(variant.portraitSpritesByClass[classIndex]);
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
