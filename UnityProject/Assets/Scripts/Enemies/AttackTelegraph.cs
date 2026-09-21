using System.Collections;
using UnityEngine;

// Red ground warning shown during an enemy's attack windup, so players can see and dodge the hit zone.
// Procedurally drawn (no art asset yet) - texture is built once and cached across every instance.
public class AttackTelegraph : MonoBehaviour
{
    private const int TextureSize = 128;
    private const string GroundSortingLayer = "Ground";
    private const float FadeInFraction = 0.25f;
    private const float PulseAmplitude = 0.12f;
    private const float PulseSpeed = 14f;

    private static Sprite cachedSprite;

    private SpriteRenderer spriteRenderer;

    // Centered on the enemy's own attack radius at the moment the windup starts - enemies stop moving
    // to attack, so a position captured once (rather than following the transform) is enough.
    public static void Show(Vector3 position, float radius, float duration)
    {
        if (radius <= 0f || duration <= 0f)
            return;

        var go = new GameObject("AttackTelegraph", typeof(SpriteRenderer));
        go.transform.position = position;
        go.transform.localScale = Vector3.one * radius;

        AttackTelegraph telegraph = go.AddComponent<AttackTelegraph>();
        telegraph.Init(duration);
    }

    private void Init(float duration)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetOrBuildSprite();
        spriteRenderer.sortingLayerName = GroundSortingLayer;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        StartCoroutine(Animate(duration));
    }

    private IEnumerator Animate(float duration)
    {
        float fadeInDuration = duration * FadeInFraction;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float fadeIn = fadeInDuration > 0f ? Mathf.Clamp01(t / fadeInDuration) : 1f;
            float pulse = 1f + Mathf.Sin(t * PulseSpeed) * PulseAmplitude;
            Color color = spriteRenderer.color;
            color.a = fadeIn * pulse;
            spriteRenderer.color = color;
            yield return null;
        }
        Destroy(gameObject);
    }

    private static Sprite GetOrBuildSprite()
    {
        if (cachedSprite != null)
            return cachedSprite;

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(TextureSize / 2f, TextureSize / 2f);
        float outerRadius = TextureSize / 2f;
        float ringStart = outerRadius * 0.85f;

        Color32[] pixels = new Color32[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = dist > ringStart ? 0.9f : 0.28f;
                alpha *= Mathf.Clamp01(outerRadius - dist);
                pixels[y * TextureSize + x] = new Color(1f, 0.15f, 0.1f, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        cachedSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize / 2f);
        return cachedSprite;
    }
}
