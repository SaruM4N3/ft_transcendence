using TMPro;
using UnityEngine;

public class CharacterCustomizationMenu : MonoBehaviour
{
    // Fallback FindAnyObjectByType because the panel starts disabled, so Awake() hasn't set instance yet.
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
    // Non-networked preview instance rendered by PreviewStage's camera onto the panel's RawImage.
    [SerializeField] private GameObject previewCharacter;

    private void Awake()
    {
        instance = this;

        if (nameInputField != null)
            nameInputField.onEndEdit.AddListener(SetPlayerName);
    }

    // Panel is re-activated (not reloaded) each time an NPC menu opens, so refresh from current state here.
    private void OnEnable()
    {
        GameObject player = LocalPlayer.Get();
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

    public void SetPlayerName(string value)
    {
        value = value.Trim();
        if (string.IsNullOrEmpty(value))
            return;
        if (value.Length > maxNameLength)
            value = value.Substring(0, maxNameLength);

        GameObject player = LocalPlayer.Get();
        if (player == null)
            return;

        PlayerCustomization customization = player.GetComponent<PlayerCustomization>();
        if (customization != null)
            customization.SetName(value);

        if (nameInputField != null && nameInputField.text != value)
            nameInputField.text = value;

        OnNameChanged?.Invoke(value);
    }

    public static event System.Action<Sprite> OnPortraitChanged;
    public static event System.Action<Sprite> OnBackgroundChanged;
    public static event System.Action<string> OnNameChanged;

    [SerializeField] private GameObject[] classPresets;
    [SerializeField] private ColorVariant[] colorVariants;
    // HUD background sword sprite per color, indexed the same as colorVariants.
    [SerializeField] private Sprite[] backgroundSpritesByColor;

    [System.Serializable]
    private class ColorVariant
    {
        public RuntimeAnimatorController[] controllersByClass;
        public Sprite[] worldSpritesByClass;
        // HUD bust icon (Human Avatars) - never shown on the world character.
        public Sprite[] portraitSpritesByClass;
    }

    // Lets the HUD sync on startup even if this menu has never been opened.
    public Sprite GetCurrentPortrait()
    {
        GameObject player = LocalPlayer.Get();
        if (player == null)
            return null;

        return GetPortrait(CurrentClassIndex(player), CurrentColorIndex(player));
    }

    // Resolves a portrait for an arbitrary class+color combo, e.g. for the lobby roster UI.
    public Sprite GetPortrait(int classIndex, int colorIndex)
    {
        if (classIndex < 0 || classIndex >= classPresets.Length)
            return null;
        if (colorIndex < 0 || colorIndex >= colorVariants.Length)
            return null;

        return colorVariants[colorIndex].portraitSpritesByClass[classIndex];
    }

    public Sprite GetCurrentBackground()
    {
        GameObject player = LocalPlayer.Get();
        if (player == null)
            return null;

        int colorIndex = CurrentColorIndex(player);
        if (colorIndex < 0 || colorIndex >= backgroundSpritesByColor.Length)
            return null;

        return backgroundSpritesByColor[colorIndex];
    }

    public string GetCurrentName()
    {
        GameObject player = LocalPlayer.Get();
        if (player == null)
            return string.Empty;

        PlayerCustomization customization = player.GetComponent<PlayerCustomization>();
        return customization != null ? customization.PlayerName : string.Empty;
    }

    public void SelectClass(int index)
    {
        if (index < 0 || index >= classPresets.Length || classPresets[index] == null)
            return;

        GameObject player = LocalPlayer.Get();
        if (player == null)
            return;

        Apply(player, index, CurrentColorIndex(player));
    }

    public void SelectColor(int index)
    {
        if (index < 0 || index >= colorVariants.Length)
            return;

        GameObject player = LocalPlayer.Get();
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

        PlayerCustomization customization = player.GetComponent<PlayerCustomization>();
        if (customization != null)
            customization.SetSelection(classIndex, colorIndex);
    }

    // Re-broadcasts to the HUD after PlayerCustomization loads a saved profile, since that happens
    // after the HUD's initial sync and bypasses Apply()/SetPlayerName().
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

    // Animator controller + world sprite only, no HUD side effects - also used to reapply a remote puppet's replicated class/color.
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

        // Controller assignment resets the animator frame synchronously, so set sprite after or it gets clobbered.
        player.GetComponent<Animator>().runtimeAnimatorController = controller;
        player.GetComponent<SpriteRenderer>().sprite = worldSprite;
        return true;
    }

    // Compares against the un-overridden base controller so a color pick never silently resets the class.
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
