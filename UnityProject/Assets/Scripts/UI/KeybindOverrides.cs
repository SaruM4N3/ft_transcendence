using UnityEngine;
using UnityEngine.InputSystem;

// Local keyboard rebind overrides, persisted via PlayerPrefs. PlayerInput clones its InputActionAsset
// at runtime, so overrides must be (re)applied to each player's own actions instance, not the project asset.
public static class KeybindOverrides
{
    private const string OverridesKey = "Settings.KeyBindingOverrides";

    public static void Apply(InputActionAsset asset)
    {
        string json = PlayerPrefs.GetString(OverridesKey, "");
        if (!string.IsNullOrEmpty(json))
            asset.LoadBindingOverridesFromJson(json);
    }

    public static void Save(InputActionAsset asset)
    {
        PlayerPrefs.SetString(OverridesKey, asset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }
}
