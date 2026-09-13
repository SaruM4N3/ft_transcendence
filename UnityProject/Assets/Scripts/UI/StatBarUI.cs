using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatBarUI : MonoBehaviour
{
    private enum Stat { Health, Mana }

    [SerializeField] private Stat stat;
    // Optional "current/max" label centered on the bar, e.g. the HealthBar's "80/100".
    [SerializeField] private TMP_Text valueText;

    private Image fillImage;

    void Awake()
    {
        fillImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (stat == Stat.Health)
            PlayerStats.OnHealthChanged += SetFill;
        else
            PlayerStats.OnManaChanged += SetFill;
    }

    void OnDisable()
    {
        if (stat == Stat.Health)
            PlayerStats.OnHealthChanged -= SetFill;
        else
            PlayerStats.OnManaChanged -= SetFill;
    }

    private void SetFill(float current, float max)
    {
        fillImage.fillAmount = max > 0f ? current / max : 0f;

        if (valueText != null)
            valueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }
}
