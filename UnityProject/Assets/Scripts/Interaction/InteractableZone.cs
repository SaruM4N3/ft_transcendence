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
