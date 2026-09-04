using System.Collections;
using UnityEngine;

// Tints the sprite briefly on a hit; auto-flashes on any health drop when an IHealthStats is on the same object.
public class SpriteHitFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float duration = 0.15f;

    private SpriteRenderer spriteRenderer;
    private IHealthStats stats;
    private float lastHealth = -1f;
    private Coroutine running;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        stats = GetComponent<IHealthStats>();
    }

    void OnEnable()
    {
        if (stats != null)
            stats.OnHealthChanged += HandleHealthChanged;
    }

    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthChanged -= HandleHealthChanged;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        running = null;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (lastHealth >= 0f && current < lastHealth)
            Flash();
        lastHealth = current;
    }

    public void Flash()
    {
        if (spriteRenderer == null || !isActiveAndEnabled)
            return;
        if (running != null)
            StopCoroutine(running);
        running = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            spriteRenderer.color = Color.Lerp(flashColor, Color.white, t / duration);
            yield return null;
        }
        spriteRenderer.color = Color.white;
        running = null;
    }
}
