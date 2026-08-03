using UnityEngine;
using UnityEngine.UI;

public class StatBarUI : MonoBehaviour
{
    private enum Stat { Health, Mana }

    [SerializeField] private Stat stat;

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
        if (max > 0f)
            fillImage.fillAmount = current / max;
        else
            fillImage.fillAmount = 0f;
    }
}
