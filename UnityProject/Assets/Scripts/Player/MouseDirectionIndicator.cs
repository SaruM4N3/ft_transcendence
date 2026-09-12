using UnityEngine;

// Ground ring at the player's feet that rotates to point at the mouse; its own child so flipX doesn't affect it.
// Reads PlayerMovement's synced aim angle rather than computing its own, so a remote puppet shows
// the same aim its owner sees instead of hiding (that owner is the only one with real mouse data).
public class MouseDirectionIndicator : MonoBehaviour
{
    // Indexed the same as CharacterCustomizationMenu's colorVariants: Black, Blue, Purple, Red, Yellow.
    [SerializeField] private Color[] colorsByColorIndex =
    {
        new Color(0.15f, 0.15f, 0.15f),
        new Color(0.25f, 0.45f, 0.95f),
        new Color(0.6f, 0.3f, 0.85f),
        new Color(0.85f, 0.25f, 0.25f),
        new Color(0.95f, 0.8f, 0.2f),
    };

    private SpriteRenderer spriteRenderer;
    private PlayerMovement playerMovement;
    private PlayerCustomization customization;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        customization = GetComponentInParent<PlayerCustomization>();
    }

    void OnEnable()
    {
        if (customization == null)
            return;

        customization.OnClassOrColorChanged += ApplyColor;
        ApplyColor(customization.ClassIndex, customization.ColorIndex);
    }

    void OnDisable()
    {
        if (customization != null)
            customization.OnClassOrColorChanged -= ApplyColor;
    }

    private void ApplyColor(int classIndex, int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= colorsByColorIndex.Length)
            return;

        spriteRenderer.color = colorsByColorIndex[colorIndex];
    }

    void Update()
    {
        // No PlayerMovement means a non-gameplay stand-in (e.g. customization preview) - nothing to aim.
        if (playerMovement == null)
            return;

        spriteRenderer.enabled = true;
        if (PauseManager.IsPaused)
            return;

        // The sprite's spike points along local +Y at rotation 0, so offset by -90 to align it
        // with the atan2 angle (which measures from +X).
        transform.rotation = Quaternion.Euler(0f, 0f, playerMovement.AimAngleDegrees - 90f);
    }
}
