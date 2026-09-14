using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Lists rebindable actions (Primary/Secondary keyboard+mouse slots, plus a Gamepad slot) and drives
// Unity's interactive rebind flow for each.
public class KeybindsPanelUI : MonoBehaviour
{
    private enum Slot { Primary, Secondary, Gamepad }
    private static readonly Slot[] AllSlots = { Slot.Primary, Slot.Secondary, Slot.Gamepad };

    [System.Serializable]
    private class Row
    {
        public string actionName;
        public string displayLabel;
        // "<Mouse>" or "<Keyboard>"; secondaryDevicePrefix is empty when the action has no second slot.
        public string primaryDevicePrefix;
        public string secondaryDevicePrefix;
        public TMP_Text primaryLabel;
        public Button primaryButton;
        public TMP_Text secondaryLabel;
        public Button secondaryButton;
        public TMP_Text gamepadLabel;
        public Button gamepadButton;
    }

    [SerializeField] private Row[] rows;
    [SerializeField] private GameObject waitingPrompt;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject settingsPanel;

    private InputActionRebindingExtensions.RebindingOperation activeRebind;

    private void Awake()
    {
        foreach (Row row in rows)
        {
            Row captured = row;
            captured.primaryButton.onClick.AddListener(() => StartRebind(captured, Slot.Primary));
            captured.secondaryButton.onClick.AddListener(() => StartRebind(captured, Slot.Secondary));
            captured.gamepadButton.onClick.AddListener(() => StartRebind(captured, Slot.Gamepad));
        }

        if (backButton != null)
            backButton.onClick.AddListener(GoBack);
    }

    private void OnEnable()
    {
        SetStatus(string.Empty, false);
        RefreshAllLabels();
    }

    private void OnDisable()
    {
        activeRebind?.Cancel();
    }

    // Wired to the waiting-for-key prompt's own Cancel button, since Escape is avoided project-wide (WebGL fullscreen).
    public void CancelActiveRebind()
    {
        activeRebind?.Cancel();
    }

    private void GoBack()
    {
        activeRebind?.Cancel();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        gameObject.SetActive(false);
    }

    private InputActionAsset CurrentAsset()
    {
        GameObject player = LocalPlayer.Get();
        PlayerInput input = player != null ? player.GetComponent<PlayerInput>() : null;
        return input != null ? input.actions : null;
    }

    private static string DevicePrefix(Row row, Slot slot)
    {
        if (slot == Slot.Primary)
            return row.primaryDevicePrefix;
        if (slot == Slot.Secondary)
            return row.secondaryDevicePrefix;
        return "<Gamepad>";
    }

    private static TMP_Text SlotLabel(Row row, Slot slot)
    {
        if (slot == Slot.Primary)
            return row.primaryLabel;
        if (slot == Slot.Secondary)
            return row.secondaryLabel;
        return row.gamepadLabel;
    }

    private static Button SlotButton(Row row, Slot slot)
    {
        if (slot == Slot.Primary)
            return row.primaryButton;
        if (slot == Slot.Secondary)
            return row.secondaryButton;
        return row.gamepadButton;
    }

    private void StartRebind(Row row, Slot slot)
    {
        if (activeRebind != null)
            return;

        string prefix = DevicePrefix(row, slot);
        if (string.IsNullOrEmpty(prefix))
            return;

        InputActionAsset asset = CurrentAsset();
        InputAction action = asset != null ? asset.FindAction(row.actionName) : null;
        int bindingIndex = FindBindingIndex(action, prefix);
        if (action == null || bindingIndex < 0)
        {
            SetStatus("Can't rebind right now.", true);
            return;
        }

        TMP_Text label = SlotLabel(row, slot);
        action.Disable();
        label.text = "...";
        if (waitingPrompt != null)
            waitingPrompt.SetActive(true);

        InputActionRebindingExtensions.RebindingOperation rebind = action.PerformInteractiveRebinding(bindingIndex);
        rebind = prefix == "<Gamepad>"
            ? rebind.WithControlsExcluding("Mouse").WithControlsExcluding("Keyboard")
            : rebind.WithControlsExcluding("Gamepad");

        activeRebind = rebind
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => FinishRebind(row, slot, action, bindingIndex, cancelled: false))
            .OnCancel(op => FinishRebind(row, slot, action, bindingIndex, cancelled: true))
            .Start();
    }

    private void FinishRebind(Row row, Slot slot, InputAction action, int bindingIndex, bool cancelled)
    {
        activeRebind?.Dispose();
        activeRebind = null;
        action.Enable();
        if (waitingPrompt != null)
            waitingPrompt.SetActive(false);

        if (!cancelled)
        {
            Row conflict = FindConflict(action.actionMap.asset, action, bindingIndex);
            if (conflict != null)
            {
                action.RemoveBindingOverride(bindingIndex);
                SetStatus("Already used by " + conflict.displayLabel + ".", true);
            }
            else
            {
                KeybindOverrides.Save(action.actionMap.asset);
                SetStatus(string.Empty, false);
            }
        }

        RefreshAllLabels();
    }

    // Effective path clash against any other rebindable slot, across every row, within the same asset.
    private Row FindConflict(InputActionAsset asset, InputAction changedAction, int changedBindingIndex)
    {
        string changedPath = changedAction.bindings[changedBindingIndex].effectivePath;

        foreach (Row row in rows)
        {
            InputAction other = asset.FindAction(row.actionName);
            if (other == null)
                continue;

            foreach (Slot slot in AllSlots)
            {
                string prefix = DevicePrefix(row, slot);
                if (string.IsNullOrEmpty(prefix))
                    continue;

                int idx = FindBindingIndex(other, prefix);
                if (idx < 0)
                    continue;
                if (other == changedAction && idx == changedBindingIndex)
                    continue;

                if (other.bindings[idx].effectivePath == changedPath)
                    return row;
            }
        }
        return null;
    }

    private static int FindBindingIndex(InputAction action, string devicePrefix)
    {
        if (action == null || string.IsNullOrEmpty(devicePrefix))
            return -1;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (!b.isComposite && !b.isPartOfComposite && b.path != null && b.path.StartsWith(devicePrefix))
                return i;
        }
        return -1;
    }

    // Unity's display string for control-character keys (Tab, Enter, ...) is the literal control
    // character, not a readable name; fall back to the binding path's last segment for those.
    private static string DisplayName(InputAction action, int bindingIndex)
    {
        string display = action.GetBindingDisplayString(bindingIndex);
        if (!string.IsNullOrEmpty(display) && display.Trim().Length > 0)
            return display;

        string path = action.bindings[bindingIndex].path;
        int slash = path.LastIndexOf('/');
        string keyName = slash >= 0 ? path.Substring(slash + 1) : path;
        return keyName.Length > 0 ? char.ToUpper(keyName[0]) + keyName.Substring(1) : "?";
    }

    private void RefreshAllLabels()
    {
        InputActionAsset asset = CurrentAsset();
        foreach (Row row in rows)
        {
            InputAction action = asset != null ? asset.FindAction(row.actionName) : null;
            foreach (Slot slot in AllSlots)
            {
                string prefix = DevicePrefix(row, slot);
                TMP_Text label = SlotLabel(row, slot);
                Button button = SlotButton(row, slot);

                if (string.IsNullOrEmpty(prefix))
                {
                    label.text = "-";
                    button.interactable = false;
                    continue;
                }

                int bindingIndex = FindBindingIndex(action, prefix);
                button.interactable = bindingIndex >= 0;
                label.text = bindingIndex >= 0 ? DisplayName(action, bindingIndex) : "-";
            }
        }
    }

    private void SetStatus(string message, bool isError)
    {
        if (statusText == null)
            return;

        statusText.text = message;
        statusText.color = isError ? new Color(0.85f, 0.2f, 0.2f) : Color.white;
    }
}
