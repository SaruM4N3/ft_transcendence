using UnityEngine;
using TMPro;

public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI Instance { get; private set; }

    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TextMeshProUGUI keyLabel;
    [SerializeField] private TextMeshProUGUI actionLabel;
    [SerializeField] private Vector2 worldOffset = new Vector2(0f, 0.9f);
    [SerializeField] private string keyGlyph = "E";

    private Camera cam;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;

        if (keyLabel != null)
            keyLabel.text = keyGlyph;

        if (promptRoot != null)
            promptRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(string text, Vector3 worldPosition)
    {
        if (actionLabel != null)
            actionLabel.text = text;

        if (promptRoot != null)
            promptRoot.gameObject.SetActive(true);

        UpdatePosition(worldPosition);
    }

    public void Hide()
    {
        if (promptRoot != null)
            promptRoot.gameObject.SetActive(false);
    }

    public void UpdatePosition(Vector3 worldPosition)
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null || promptRoot == null)
            return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition + (Vector3)worldOffset);
        promptRoot.position = screenPos;
    }
}
