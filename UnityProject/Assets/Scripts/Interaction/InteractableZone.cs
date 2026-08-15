using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class InteractableZone : MonoBehaviour
{
    [SerializeField] private string promptText = "Enter";
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private UnityEvent onInteract;

    public string PromptText => promptText;
    public Vector3 PromptPosition => promptAnchor != null ? promptAnchor.position : transform.position;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            InteractionManager.Register(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            InteractionManager.Unregister(this);
    }

    public void Interact()
    {
        onInteract?.Invoke();
    }
}
