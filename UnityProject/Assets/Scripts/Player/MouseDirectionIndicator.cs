using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Ground ring at the player's feet that rotates to point at the mouse; its own child so flipX doesn't affect it.
// Not a NetworkBehaviour - the preview stand-in has no NetworkObject, so networkObject can be null.
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
    private NetworkObject networkObject;
    private PlayerCustomization customization;
    private Camera cam;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        networkObject = GetComponentInParent<NetworkObject>();
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
        // No NetworkObject means a non-gameplay stand-in (e.g. customization preview) - nothing to aim.
        if (networkObject == null)
            return;

        bool locallyControlled = !networkObject.IsSpawned || networkObject.IsOwner;
        if (!locallyControlled)
        {
            // Remote puppets have no local mouse data to show their aim with, so just hide it.
            spriteRenderer.enabled = false;
            return;
        }
        spriteRenderer.enabled = true;

        if (PauseManager.IsPaused)
            return;

        if (cam == null)
            cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return;

        Vector3 mouseScreen = Mouse.current.position.ReadValue();
        mouseScreen.z = -cam.transform.position.z;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

        Vector2 direction = (Vector2)mouseWorld - (Vector2)transform.position;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // The sprite's spike points along local +Y at rotation 0, so offset by -90 to align it
        // with the atan2 angle (which measures from +X).
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
