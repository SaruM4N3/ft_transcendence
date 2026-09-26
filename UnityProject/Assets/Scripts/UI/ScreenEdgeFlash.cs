using UnityEngine;
using UnityEngine.UI;

// Full-screen red vignette that pulses on demand; builds its own overlay canvas and gradient at runtime.
public class ScreenEdgeFlash : MonoBehaviour
{
    private const int TextureSize = 128;

    private Image image;
    private float intensity;
    private float fadeSpeed = 3f;

    public static ScreenEdgeFlash Create()
    {
        var root = new GameObject("ScreenEdgeFlash");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var flash = root.AddComponent<ScreenEdgeFlash>();
        var imageGO = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageGO.transform.SetParent(root.transform, false);
        var rect = (RectTransform)imageGO.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        flash.image = imageGO.GetComponent<Image>();
        flash.image.raycastTarget = false;
        flash.image.sprite = BuildVignetteSprite();
        flash.image.color = new Color(1f, 0.05f, 0.05f, 0f);
        return flash;
    }

    public void Pulse(float strength, float duration)
    {
        intensity = Mathf.Max(intensity, strength);
        fadeSpeed = strength / Mathf.Max(0.05f, duration);
    }

    void Update()
    {
        if (image == null)
            return;
        intensity = Mathf.MoveTowards(intensity, 0f, fadeSpeed * Time.unscaledDeltaTime);
        Color c = image.color;
        c.a = intensity;
        image.color = c;
    }

    private static Sprite BuildVignetteSprite()
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float dx = (x / (TextureSize - 1f)) * 2f - 1f;
                float dy = (y / (TextureSize - 1f)) * 2f - 1f;
                float alpha = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.85f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f));
    }
}
