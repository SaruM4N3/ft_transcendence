using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// World Space health bar bound to anything implementing IHealthStats (DummyStats, EnemyStats),
// same per-instance-binding idea as LobbyRosterEntryUI (not the local-player-only static events StatBarUI uses).
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    // MonoBehaviour, not IHealthStats directly - Unity can't serialize a plain interface reference.
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

    // Awake (where the stats source sets its initial CurrentHealth) is guaranteed to run for every
    // object before any Start, so the initial sync belongs here rather than in OnEnable.
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
