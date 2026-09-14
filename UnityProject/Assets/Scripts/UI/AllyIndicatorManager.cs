using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Screen-edge arrows pointing toward allies currently outside the local camera's view.
public class AllyIndicatorManager : MonoBehaviour
{
    private const float EdgeMargin = 40f;
    private const int ArrowTextureSize = 32;
    private const float DirectionSmoothing = 8f;

    private static readonly Color[] ColorsByColorIndex =
    {
        new Color(0.15f, 0.15f, 0.15f), //Black
        new Color(0.25f, 0.45f, 0.95f), //Blue
        new Color(0.6f, 0.3f, 0.85f), //Purple
        new Color(0.85f, 0.25f, 0.25f), //Red
        new Color(0.95f, 0.8f, 0.2f), //Yellow
    };

    private class IndicatorState
    {
        public RectTransform Rect;
        public Vector2 SmoothedDir;
        public bool HasDir;
    }

    private RectTransform canvasRect;
    private Sprite arrowSprite;
    private readonly Dictionary<PlayerCustomization, IndicatorState> indicators = new Dictionary<PlayerCustomization, IndicatorState>();

    private void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
        arrowSprite = BuildArrowSprite();
    }

    private void OnEnable()
    {
        PlayerCustomization.OnPlayerRegistered += HandleRegistered;
        PlayerCustomization.OnPlayerUnregistered += HandleUnregistered;

        foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
            HandleRegistered(player);
    }

    private void OnDisable()
    {
        PlayerCustomization.OnPlayerRegistered -= HandleRegistered;
        PlayerCustomization.OnPlayerUnregistered -= HandleUnregistered;

        foreach (IndicatorState indicator in indicators.Values)
            if (indicator.Rect != null)
                Destroy(indicator.Rect.gameObject);
        indicators.Clear();
    }

    private void HandleRegistered(PlayerCustomization player)
    {
        if (player == null || player.IsLocallyControlled() || indicators.ContainsKey(player))
            return;

        GameObject go = new GameObject("AllyIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(canvasRect, false);
        rect.sizeDelta = new Vector2(28f, 28f);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);

        Image image = go.GetComponent<Image>();
        image.sprite = arrowSprite;
        image.raycastTarget = false;
        image.color = ColorsByColorIndex[Mathf.Clamp(player.ColorIndex, 0, ColorsByColorIndex.Length - 1)];

        go.SetActive(false);
        indicators.Add(player, new IndicatorState { Rect = rect });
    }

    private void HandleUnregistered(PlayerCustomization player)
    {
        if (player == null || !indicators.TryGetValue(player, out IndicatorState indicator))
            return;

        if (indicator.Rect != null)
            Destroy(indicator.Rect.gameObject);
        indicators.Remove(player);
    }

    private void Update()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector2 halfSize = canvasRect.rect.size * 0.5f - Vector2.one * EdgeMargin;

        foreach (KeyValuePair<PlayerCustomization, IndicatorState> pair in indicators)
        {
            PlayerCustomization ally = pair.Key;
            IndicatorState indicator = pair.Value;
            if (ally == null || indicator.Rect == null)
                continue;

            UpdateIndicator(cam, ally.transform, indicator, halfSize);
        }
    }

    // The raw direction to an off-screen ally is noisy frame to frame (NetworkTransform interpolation
    // jitter), and near an edge/corner that noise flips which axis the clamp binds to - smoothing the
    // direction itself (not just the final position) keeps the arrow from vibrating along the edge.
    private static void UpdateIndicator(Camera cam, Transform ally, IndicatorState indicator, Vector2 halfSize)
    {
        RectTransform rect = indicator.Rect;
        Vector3 viewportPoint = cam.WorldToViewportPoint(ally.position);
        bool behindCamera = viewportPoint.z < 0f;
        bool onScreen = !behindCamera && viewportPoint.x > 0f && viewportPoint.x < 1f && viewportPoint.y > 0f && viewportPoint.y < 1f;

        if (rect.gameObject.activeSelf != !onScreen)
            rect.gameObject.SetActive(!onScreen);
        if (onScreen)
        {
            indicator.HasDir = false;
            return;
        }

        Vector2 rawDir = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
        if (behindCamera)
            rawDir = -rawDir;
        if (rawDir.sqrMagnitude < 0.0001f)
            rawDir = Vector2.up;
        rawDir.Normalize();

        if (!indicator.HasDir)
        {
            indicator.SmoothedDir = rawDir;
            indicator.HasDir = true;
        }
        else
        {
            float t = 1f - Mathf.Exp(-DirectionSmoothing * Time.deltaTime);
            indicator.SmoothedDir = Vector2.Lerp(indicator.SmoothedDir, rawDir, t).normalized;
        }
        Vector2 dir = indicator.SmoothedDir;

        float scaleX = halfSize.x / Mathf.Max(Mathf.Abs(dir.x), 0.0001f);
        float scaleY = halfSize.y / Mathf.Max(Mathf.Abs(dir.y), 0.0001f);
        rect.anchoredPosition = dir * Mathf.Min(scaleX, scaleY);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // A simple upward-pointing triangle, alpha-masked; no matching art asset exists to reuse.
    private static Sprite BuildArrowSprite()
    {
        Texture2D tex = new Texture2D(ArrowTextureSize, ArrowTextureSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < ArrowTextureSize; y++)
        {
            float ny = y / (float)(ArrowTextureSize - 1);
            for (int x = 0; x < ArrowTextureSize; x++)
            {
                float nx = (x - ArrowTextureSize * 0.5f) / (ArrowTextureSize * 0.5f);
                bool inside = Mathf.Abs(nx) <= 1f - ny;
                tex.SetPixel(x, y, inside ? Color.white : clear);
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, ArrowTextureSize, ArrowTextureSize), new Vector2(0.5f, 0.5f));
    }
}
