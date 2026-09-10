using TMPro;
using UnityEngine;

// Top-center Coop HUD: survival timer and current wave, shown once the waves start.
public class CoopHUD : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text waveText;

    private WaveSpawner spawner;
    private GameOverCheck gameOver;
    private bool frozen;
    private float frozenElapsed;
    private int shownSeconds = -1;
    private int shownWave = -1;

    void Awake()
    {
        content.SetActive(false);
    }

    void Update()
    {
        if (spawner == null)
        {
            spawner = FindAnyObjectByType<WaveSpawner>();
            gameOver = FindAnyObjectByType<GameOverCheck>();
            if (spawner == null)
                return;
        }

        bool started = spawner.CurrentWave > 0;
        if (content.activeSelf != started)
            content.SetActive(started);

        if (!started)
            return;

        FreezeOnGameOver();
        Refresh(frozen ? frozenElapsed : spawner.ElapsedSeconds, spawner.CurrentWave);
    }

    // Stops the clock at the wipe so the final time stays readable.
    private void FreezeOnGameOver()
    {
        if (frozen || gameOver == null || !gameOver.IsGameOver)
            return;

        frozen = true;
        frozenElapsed = spawner.ElapsedSeconds;
    }

    // Text only changes once per second or per wave, so nothing allocates per frame.
    private void Refresh(float elapsed, int wave)
    {
        int seconds = Mathf.Max(0, Mathf.FloorToInt(elapsed));
        if (seconds != shownSeconds)
        {
            shownSeconds = seconds;
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        if (wave != shownWave)
        {
            shownWave = wave;
            waveText.text = $"Wave {wave}";
        }
    }
}
