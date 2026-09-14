using UnityEngine;
using UnityEngine.UI;

// Settings panel: gameplay preference toggles, synced from GameSettings whenever the panel opens.
public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Toggle enemyHealthBarsToggle;
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Button keybindsButton;
    [SerializeField] private GameObject keybindsPanel;

    private void Awake()
    {
        enemyHealthBarsToggle.onValueChanged.AddListener(value => GameSettings.ShowEnemyHealthBars = value);
        screenShakeToggle.onValueChanged.AddListener(value => GameSettings.ScreenShakeEnabled = value);

        if (keybindsButton != null && keybindsPanel != null)
            keybindsButton.onClick.AddListener(() =>
            {
                keybindsPanel.SetActive(true);
                gameObject.SetActive(false);
            });
    }

    private void OnEnable()
    {
        enemyHealthBarsToggle.SetIsOnWithoutNotify(GameSettings.ShowEnemyHealthBars);
        screenShakeToggle.SetIsOnWithoutNotify(GameSettings.ScreenShakeEnabled);
    }
}
