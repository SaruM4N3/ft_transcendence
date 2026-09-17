using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class InteractableZone : MonoBehaviour
{
    [SerializeField] private string promptText = "Enter";
    [SerializeField] private InteractPromptUI promptUI;
    [SerializeField] private UnityEvent onInteract;

    public string PromptText => promptText;
    public Vector3 Position => transform.position;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // ActiveZones is one shared registry: registering any remote player's collider would let
    // one player's movement add/remove zones out from under every other player's nearest-zone check.
    private static bool IsLocalPlayer(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return false;

        NetworkObject networkObject = other.GetComponentInParent<NetworkObject>();
        return networkObject == null || !networkObject.IsSpawned || networkObject.IsOwner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsLocalPlayer(other))
            InteractionManager.Register(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsLocalPlayer(other))
            InteractionManager.Unregister(this);
    }

    public void ShowPrompt()
    {
        if (promptUI != null)
            promptUI.Show(promptText);
    }

    public void HidePrompt()
    {
        if (promptUI != null)
            promptUI.Hide();
    }

    public void Interact()
    {
        onInteract?.Invoke();
    }
}
