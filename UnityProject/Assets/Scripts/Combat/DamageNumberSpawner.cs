using UnityEngine;

// Spawns a floating damage number FX each time this enemy's health drops.
public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);

    private IHealthStats stats;
    private float lastHealth = -1f;

    void Awake()
    {
        stats = GetComponent<IHealthStats>();
    }

    void OnEnable()
    {
        if (stats != null)
            stats.OnHealthChanged += HandleHealthChanged;
        lastHealth = -1f;
    }

    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (lastHealth >= 0f && current < lastHealth && GameSettings.ShowDamageNumbers)
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
