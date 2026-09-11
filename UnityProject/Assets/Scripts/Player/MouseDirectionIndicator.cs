using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Ground-level ring at the local player's feet that rotates to point at the mouse. Own child GameObject
// so rotation is independent of the body sprite's flipX facing. Plain MonoBehaviour, not NetworkBehaviour:
// the customization preview stand-in has NetworkObject removed but still gets this child, so networkObject
// can legitimately be null there.
public class MouseDirectionIndicator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private NetworkObject networkObject;
    private Camera cam;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        networkObject = GetComponentInParent<NetworkObject>();
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
