using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

// Corner overlay: FPS/ping readouts, independently toggled from GameSettings.
public class PerfStatsHUD : MonoBehaviour
{
    [SerializeField] private GameObject fpsRow;
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private GameObject pingRow;
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private float refreshInterval = 0.5f;

    private float refreshTimer;
    private int framesSinceRefresh;
    private int shownFps = -1;
    private int shownPing = -2;

    private void OnEnable()
    {
        GameSettings.OnChanged += ApplyVisibility;
        ApplyVisibility();
    }

    private void OnDisable()
    {
        GameSettings.OnChanged -= ApplyVisibility;
    }

    private void ApplyVisibility()
    {
        fpsRow.SetActive(GameSettings.ShowFps);
        pingRow.SetActive(GameSettings.ShowPing);
    }

    private void Update()
    {
        framesSinceRefresh++;
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < refreshInterval)
            return;

        if (GameSettings.ShowFps)
            RefreshFps(framesSinceRefresh / refreshTimer);
        if (GameSettings.ShowPing)
            RefreshPing();

        refreshTimer = 0f;
        framesSinceRefresh = 0;
    }

    private void RefreshFps(float fps)
    {
        int rounded = Mathf.RoundToInt(fps);
        if (rounded == shownFps)
            return;

        shownFps = rounded;
        fpsText.text = $"FPS {rounded}";
    }

    private void RefreshPing()
    {
        int ms = CurrentPingMs();
        if (ms == shownPing)
            return;

        shownPing = ms;
        pingText.text = ms >= 0 ? $"Ping {ms}ms" : "Ping --";
    }

    // -1 offline/host-only, where RTT to a server has no meaning.
    private static int CurrentPingMs()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsClient || !manager.IsConnectedClient || manager.IsServer)
            return -1;

        if (manager.NetworkConfig.NetworkTransport is not UnityTransport transport)
            return -1;

        return (int)transport.GetCurrentRtt(NetworkManager.ServerClientId);
    }
}
