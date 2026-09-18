using UnityEngine;

// Local, per-client gameplay preferences; not synced to other players.
public static class GameSettings
{
    private const string ShowEnemyHealthBarsKey = "Settings.ShowEnemyHealthBars";
    private const string ScreenShakeEnabledKey = "Settings.ScreenShakeEnabled";
    private const string ShowDamageNumbersKey = "Settings.ShowDamageNumbers";

    public static event System.Action OnChanged;

    public static bool ShowEnemyHealthBars
    {
        get => PlayerPrefs.GetInt(ShowEnemyHealthBarsKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ShowEnemyHealthBarsKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool ScreenShakeEnabled
    {
        get => PlayerPrefs.GetInt(ScreenShakeEnabledKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ScreenShakeEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool ShowDamageNumbers
    {
        get => PlayerPrefs.GetInt(ShowDamageNumbersKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ShowDamageNumbersKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }
}
