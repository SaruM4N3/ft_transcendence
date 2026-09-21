using UnityEngine;

// Ground ring at the player's feet that rotates to point at the mouse; its own child so flipX doesn't affect it.
public class MouseDirectionIndicator : MonoBehaviour
{
    [SerializeField] private Color[] colorsByColorIndex =
    {
        new Color(0.15f, 0.15f, 0.15f), //Black
        new Color(0.25f, 0.45f, 0.95f), //Blue
        new Color(0.6f, 0.3f, 0.85f), //Purple
        new Color(0.85f, 0.25f, 0.25f), //Red
        new Color(0.95f, 0.8f, 0.2f), //Yellow
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
        if (playerMovement == null)
            return;

        spriteRenderer.enabled = true;
        if (PauseManager.IsPaused)
            return;

        transform.rotation = Quaternion.Euler(0f, 0f, playerMovement.AimAngleDegrees - 90f);
    }
}
