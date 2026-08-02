using UnityEngine;
using UnityEngine.UI;

public class CooldownIcon : MonoBehaviour
{
    [SerializeField] private AbilityType ability;

    private Image overlay;
    private float duration;
    private float remaining;

    void Awake()
    {
        overlay = GetComponent<Image>();
        overlay.fillAmount = 0f;
    }

    void OnEnable()
    {
        PlayerMovement.OnAbilityUsed += HandleAbilityUsed;
    }

    void OnDisable()
    {
        PlayerMovement.OnAbilityUsed -= HandleAbilityUsed;
    }

    void Update()
    {
        if (remaining <= 0f)
            return;

        remaining -= Time.deltaTime;

        if (duration > 0f)
            overlay.fillAmount = Mathf.Clamp01(remaining / duration);
        else
            overlay.fillAmount = 0f;
    }

    private void HandleAbilityUsed(AbilityType usedAbility, float cooldownDuration)
    {
        if (usedAbility != ability)
            return;

        duration = cooldownDuration;
        remaining = cooldownDuration;
        overlay.fillAmount = 1f;
    }
}
