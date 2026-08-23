using UnityEngine;
using UnityEngine.UI;

// World Space health bar bound to a specific DummyStats, same per-instance-binding idea as
// LobbyRosterEntryUI (not the local-player-only static events StatBarUI uses).
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private DummyStats stats;

    void OnEnable()
    {
        if (stats != null)
            stats.OnHealthChanged += SetFill;
    }

    // Awake (where DummyStats sets its initial CurrentHealth) is guaranteed to run for every
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
