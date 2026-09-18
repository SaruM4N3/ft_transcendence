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

    // Current display string for an action's keyboard binding (e.g. "E"), or null if unbound/unavailable.
    public static string GetKeyboardDisplay(string actionName)
    {
        GameObject player = LocalPlayer.Get();
        PlayerInput input = player != null ? player.GetComponent<PlayerInput>() : null;
        InputAction action = input != null ? input.actions?.FindAction(actionName) : null;
        if (action == null)
            return null;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite || binding.path == null || !binding.path.StartsWith("<Keyboard>"))
                continue;

            // Unity's display string for control-character keys (Tab, Enter, ...) is the literal control
            // character, not a readable name; fall back to the binding path's last segment for those.
            string display = action.GetBindingDisplayString(i);
            if (!string.IsNullOrEmpty(display) && display.Trim().Length > 0)
                return display;

            string path = binding.path;
            int slash = path.LastIndexOf('/');
            string keyName = slash >= 0 ? path.Substring(slash + 1) : path;
            return keyName.Length > 0 ? char.ToUpper(keyName[0]) + keyName.Substring(1) : "?";
        }
        return null;
    }
}
