using Fusion;
using UnityEngine;
using TMPro;
using System.Linq; // necesario para .Count()

public class NetworkMetricsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI metricsText;
    [SerializeField] private bool showMetrics = true;
    [SerializeField] private float updateInterval = 0.5f;

    private NetworkRunner runner;
    private float lastUpdateTime = 0f;

    private void Start()
    {
        runner = FindFirstObjectByType<NetworkRunner>();

        if (metricsText == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                GameObject metricsGo = new GameObject("NetworkMetrics");
                metricsGo.transform.SetParent(canvas.transform, false);
                metricsText = metricsGo.AddComponent<TextMeshProUGUI>();
                RectTransform rect = metricsText.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(0.3f, 0.2f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                metricsText.fontSize = 20;
                metricsText.alignment = TextAlignmentOptions.BottomLeft;
            }
        }
    }

    private void Update()
    {
        if (!showMetrics || metricsText == null) return;

        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateMetrics();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateMetrics()
{
    if (runner == null) return;

    // En Fusion 2 el ping se obtiene así
    int ping = 0;

    int remotePlayers = Mathf.Max(0, runner.ActivePlayers.Count() - 1);
    string status = runner.IsRunning ? "Connected" : "Disconnected";

    string metricsInfo = "=== NETWORK METRICS ===\n" +
        $"Ping: {ping}ms\n" +
        $"Tick: {runner.Tick}\n" +
        $"Remote Players: {remotePlayers}\n" +
        $"Status: {status}\n";

    if (runner.LocalPlayer != default)
    {
        metricsInfo += $"Local Player: {runner.LocalPlayer.PlayerId}\n";
        metricsInfo += $"Is MasterClient: {runner.IsSharedModeMasterClient}\n";
    }

    metricsText.text = metricsInfo;
}
}