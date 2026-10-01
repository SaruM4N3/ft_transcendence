using UnityEngine;
using UnityEngine.UI;

// Settings panel: gameplay preference toggles, synced from GameSettings whenever the panel opens.
public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Toggle enemyHealthBarsToggle;
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Toggle damageNumbersToggle;
    [SerializeField] private Toggle showFpsToggle;
    [SerializeField] private Toggle showPingToggle;

    private void Awake()
    {
        enemyHealthBarsToggle.onValueChanged.AddListener(value => GameSettings.ShowEnemyHealthBars = value);
        screenShakeToggle.onValueChanged.AddListener(value => GameSettings.ScreenShakeEnabled = value);
        damageNumbersToggle.onValueChanged.AddListener(value => GameSettings.ShowDamageNumbers = value);
        showFpsToggle.onValueChanged.AddListener(value => GameSettings.ShowFps = value);
        showPingToggle.onValueChanged.AddListener(value => GameSettings.ShowPing = value);
    }

    private void OnEnable()
    {
        enemyHealthBarsToggle.SetIsOnWithoutNotify(GameSettings.ShowEnemyHealthBars);
        screenShakeToggle.SetIsOnWithoutNotify(GameSettings.ScreenShakeEnabled);
        damageNumbersToggle.SetIsOnWithoutNotify(GameSettings.ShowDamageNumbers);
        showFpsToggle.SetIsOnWithoutNotify(GameSettings.ShowFps);
        showPingToggle.SetIsOnWithoutNotify(GameSettings.ShowPing);
    }
}
