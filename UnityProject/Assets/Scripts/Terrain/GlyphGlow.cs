using UnityEngine;
using UnityEngine.Rendering.Universal;

// Pulses alpha, scale and an optional child Light2D for a glowing rune overlay, looping continuously.
public class GlyphGlow : MonoBehaviour
{
    [SerializeField] private float minAlpha = 0.45f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float scalePulse = 0.08f;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float timeOffset = 0f;
    [SerializeField] private float minLightIntensity = 0.6f;
    [SerializeField] private float maxLightIntensity = 1.4f;

    private SpriteRenderer spriteRenderer;
    private Light2D light2D;
    private Vector3 baseScale;
    private Color baseColor;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        light2D = GetComponentInChildren<Light2D>();
        baseScale = transform.localScale;
        baseColor = spriteRenderer.color;
    }

    void Update()
    {
        if (!spriteRenderer.isVisible)
            return;

        float t = (Mathf.Sin((Time.time + timeOffset) * pulseSpeed) + 1f) * 0.5f;
        Color c = baseColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        spriteRenderer.color = c;
        transform.localScale = baseScale * (1f + scalePulse * t);

        if (light2D != null)
            light2D.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, t);
    }
}
