using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// World-space health bar bound to any IHealthStats.
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [FormerlySerializedAs("stats")]
    [SerializeField] private MonoBehaviour statsSource;

    private IHealthStats stats;
    private Canvas canvas;

    // statsSource is a MonoBehaviour since Unity can't serialize interface references directly.
    void Awake()
    {
        stats = statsSource as IHealthStats;
        canvas = GetComponent<Canvas>();
    }

    void OnEnable()
    {
        if (stats != null)
            stats.OnHealthChanged += SetFill;
        GameSettings.OnChanged += ApplyVisibility;
        ApplyVisibility();
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
        GameSettings.OnChanged -= ApplyVisibility;
    }

    private void ApplyVisibility()
    {
        if (canvas != null)
            canvas.enabled = GameSettings.ShowEnemyHealthBars;
    }

    private void SetFill(float current, float max)
    {
        fillImage.fillAmount = max > 0f ? current / max : 0f;
    }
}
