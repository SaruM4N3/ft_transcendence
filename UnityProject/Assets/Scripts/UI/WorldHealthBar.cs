using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// World-space health bar bound to any IHealthStats.
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    // A MonoBehaviour, since Unity can't serialize interface references.
    [FormerlySerializedAs("stats")]
    [SerializeField] private MonoBehaviour statsSource;

    private IHealthStats stats;

    void Awake()
    {
        stats = statsSource as IHealthStats;
    }

    void OnEnable()
    {
        if (stats != null)
            stats.OnHealthChanged += SetFill;
    }

    // Initial sync happens here, after every Awake has set initial health.
    void Start()
    {
        if (stats != null)
            SetFill(stats.CurrentHealth, stats.MaxHealth);
    }

    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthChanged -= SetFill;
    }

    private void SetFill(float current, float max)
    {
        fillImage.fillAmount = max > 0f ? current / max : 0f;
    }
}
