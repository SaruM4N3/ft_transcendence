using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Level-up choice screen: solo has no timer; multiplayer adds a countdown and a side ready list.
public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button[] bonusButtons;
    [SerializeField] private GameObject timerGroup;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject readyListGroup;
    [SerializeField] private TMP_Text readyListText;
    [SerializeField] private TMP_Text waitingText;

    private readonly StringBuilder readyListBuilder = new StringBuilder();

    private LevelUpManager manager;
    private bool localChosen;
    private bool shown;

    public static bool IsShowing { get; private set; }

    void Awake()
    {
        IsShowing = false;
        panel.SetActive(false);

        for (int i = 0; i < bonusButtons.Length; i++)
        {
            int bonusIndex = i;
            bonusButtons[i].onClick.AddListener(() => ChooseBonus(bonusIndex));
        }
    }

    void OnDestroy()
    {
        IsShowing = false;
    }

    void Update()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<LevelUpManager>();
            if (manager == null)
                return;
        }

        bool active = manager.SequenceActive;
        if (active != shown)
        {
            shown = active;
            IsShowing = active;
            panel.SetActive(active);
            PauseManager.SetExternalPause(active);
            if (active)
                BeginShow();
        }

        if (!active)
            return;

        bool multi = Player.AllActiveInstances.Count > 1;
        timerGroup.SetActive(multi);
        readyListGroup.SetActive(multi);

        if (multi)
        {
            timerText.text = Mathf.CeilToInt(manager.SecondsRemaining).ToString();
            RefreshReadyList();
        }

        waitingText.gameObject.SetActive(localChosen);
    }

    private void BeginShow()
    {
        localChosen = false;
        levelText.text = $"Level {manager.SequenceLevel}";
        waitingText.gameObject.SetActive(false);
        SetButtonsInteractable(true);
    }

    private void ChooseBonus(int bonusIndex)
    {
        if (localChosen)
            return;

        Player local = LocalPlayer.GetPlayer();
        if (local == null)
            return;

        local.ChooseLevelUpBonus(bonusIndex);
        localChosen = true;
        SetButtonsInteractable(false);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (Button button in bonusButtons)
            button.interactable = interactable;
    }

    private void RefreshReadyList()
    {
        readyListBuilder.Clear();
        foreach (Player player in Player.AllActiveInstances)
            readyListBuilder.AppendLine($"{player.PlayerName}: {(player.HasChosenLevelUpBonus ? "Ready" : "...")}");
        readyListText.text = readyListBuilder.ToString();
    }
}
