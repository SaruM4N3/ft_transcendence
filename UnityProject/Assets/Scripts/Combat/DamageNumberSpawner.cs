using System.Collections;
using UnityEngine;

// Spawns a floating damage number FX each time this actor's health drops.
public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);

    private IHealthStats stats;
    private float lastHealth;

    void Awake()
    {
        stats = GetComponent<IHealthStats>();
    }

    void OnEnable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged += HandleHealthChanged;
            StartCoroutine(CaptureBaselineNextFrame());
        }
    }

    // Deferred a frame: OnEnable can run before a sibling component's Awake sets the real starting health.
    private IEnumerator CaptureBaselineNextFrame()
    {
        yield return null;
        lastHealth = stats.CurrentHealth;
    }

    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (current < lastHealth && GameSettings.ShowDamageNumbers)
            Spawn(lastHealth - current);
        lastHealth = current;
    }

    private void Spawn(float amount)
    {
        if (damageNumberPrefab == null)
            return;

        GameObject fx = Instantiate(damageNumberPrefab, transform.position + spawnOffset, Quaternion.identity);
        fx.GetComponent<DamageNumberPopup>()?.Show(amount);
    }
}
