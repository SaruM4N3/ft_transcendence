using UnityEngine;
using UnityEngine.UI;

// Drives a settings tab bar: one page visible at a time, active tab button non-interactable to read as "selected".
public class SettingsTabGroup : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public Button tabButton;
        public GameObject page;
    }

    [SerializeField] private Tab[] tabs;

    private void Awake()
    {
        foreach (Tab tab in tabs)
        {
            Tab captured = tab;
            captured.tabButton.onClick.AddListener(() => Select(captured));
        }
    }

    private void OnEnable()
    {
        Select(tabs[0]);
    }

    private void Select(Tab selected)
    {
        foreach (Tab tab in tabs)
        {
            tab.page.SetActive(tab == selected);
            tab.tabButton.interactable = tab != selected;
        }
    }
}
