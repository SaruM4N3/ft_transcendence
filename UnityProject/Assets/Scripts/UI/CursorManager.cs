using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Replaces the OS cursor with a UI-rendered one, spawned once at startup (before any scene
/// loads, so no GameObject needs to be wired into every scene) and kept alive across scenes. Needed
/// because the hardware cursor (Cursor.SetCursor) can't be smoothly resized without regenerating its
/// texture every frame - rendering our own lets the click feedback below just scale a RectTransform,
/// and lets it swap to a different sprite while hovering a clickable UI element.</summary>
public class CursorManager : MonoBehaviour
{
    // Both sprites' hotspots are the arrow/hand tip's position within the *full* 64x64 source
    // texture, top-left origin. Computed relative to each sprite's own rect at runtime (see
    // ApplySprite) rather than hardcoded as a pivot, since the sprites' .meta crop isn't actually
    // applied - Resources.Load still returns the full untrimmed texture.
    private static readonly Vector2 DefaultHotspot = new Vector2(21, 16);
    private static readonly Vector2 HoverHotspot = new Vector2(19, 16);
    private const string DefaultCursorResourcePath = "Tiny Swords/UI Elements/Cursors/Cursor_01";
    private const string HoverCursorResourcePath = "Tiny Swords/UI Elements/Cursors/Cursor_02";
    private const int CursorSortingOrder = 10000;

    [SerializeField] private float displayScale = 0.5f;
    [SerializeField] private float clickScale = 0.5f;
    [SerializeField] private float clickAnimationDuration = 0.08f;

    private Sprite defaultSprite;
    private Sprite hoverSprite;
    private RectTransform canvasRect;
    private RectTransform cursorRect;
    private Image cursorImage;
    private Coroutine activeAnimation;
    private bool isHoveringClickable;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("CursorManager");
        DontDestroyOnLoad(go);
        go.AddComponent<CursorManager>();
    }

    private void Awake()
    {
        defaultSprite = Resources.Load<Sprite>(DefaultCursorResourcePath);
        hoverSprite = Resources.Load<Sprite>(HoverCursorResourcePath);
        if (defaultSprite == null)
        {
            Debug.LogWarning($"CursorManager: could not load cursor sprite at Resources/{DefaultCursorResourcePath}");
            Destroy(gameObject);
            return;
        }

        Cursor.visible = false;

        var canvasGO = new GameObject("CursorCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CursorSortingOrder;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        canvasRect = canvasGO.GetComponent<RectTransform>();

        var imageGO = new GameObject("Cursor", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);
        cursorImage = imageGO.GetComponent<Image>();
        cursorImage.raycastTarget = false;
        cursorRect = imageGO.GetComponent<RectTransform>();
        cursorRect.anchorMin = cursorRect.anchorMax = new Vector2(0.5f, 0.5f);

        ApplySprite(defaultSprite, DefaultHotspot);
    }

    // Swaps the displayed sprite and recomputes the pivot for its hotspot, since the two cursor arts
    // don't share the same tip position within their source texture.
    private void ApplySprite(Sprite sprite, Vector2 hotspot)
    {
        cursorImage.sprite = sprite;
        cursorImage.SetNativeSize();
        cursorRect.sizeDelta *= displayScale;

        Rect spriteRect = sprite.rect;
        float spriteTopFromTextureTop = sprite.texture.height - (spriteRect.y + spriteRect.height);
        cursorRect.pivot = new Vector2(
            (hotspot.x - spriteRect.x) / spriteRect.width,
            1f - (hotspot.y - spriteTopFromTextureTop) / spriteRect.height);
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }

    private void Update()
    {
        if (cursorRect == null || Mouse.current == null)
            return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        // Outside the game window (or the app isn't focused), let the real OS cursor take back over
        // instead of leaving our UI cursor frozen at the last in-bounds position.
        bool insideWindow = Application.isFocused
            && mouseScreenPosition.x >= 0f && mouseScreenPosition.x <= Screen.width
            && mouseScreenPosition.y >= 0f && mouseScreenPosition.y <= Screen.height;
        Cursor.visible = !insideWindow;
        cursorImage.enabled = insideWindow;
        if (!insideWindow)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPosition, null, out Vector2 localPoint))
            cursorRect.localPosition = localPoint;

        if (hoverSprite != null)
        {
            bool nowHovering = IsPointerOverClickable(mouseScreenPosition);
            if (nowHovering != isHoveringClickable)
            {
                isHoveringClickable = nowHovering;
                ApplySprite(nowHovering ? hoverSprite : defaultSprite, nowHovering ? HoverHotspot : DefaultHotspot);
            }
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (activeAnimation != null)
                StopCoroutine(activeAnimation);
            activeAnimation = StartCoroutine(AnimateClick());
        }
    }

    // True if the topmost UI element under the pointer is an interactable Selectable (Button,
    // Toggle, etc.) - covers menu buttons without hardcoding to the Button type specifically.
    private bool IsPointerOverClickable(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var pointerEventData = new PointerEventData(EventSystem.current) { position = screenPosition };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        foreach (RaycastResult result in raycastResults)
        {
            Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.interactable)
                return true;
        }
        return false;
    }

    // Quick shrink-and-recover pulse on unscaled time so it still plays while the game is paused.
    private IEnumerator AnimateClick()
    {
        float t = 0f;
        while (t < clickAnimationDuration)
        {
            t += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(t / clickAnimationDuration) * Mathf.PI);
            cursorRect.localScale = Vector3.one * Mathf.Lerp(1f, clickScale, pulse);
            yield return null;
        }

        cursorRect.localScale = Vector3.one;
        activeAnimation = null;
    }
}
