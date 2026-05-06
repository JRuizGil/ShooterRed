using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Muestra la tabla de posiciones con kills, deaths y rachas
public class LeaderboardUI : MonoBehaviour
{
    [Header("Leaderboard Display")]
    [SerializeField] private TextMeshProUGUI leaderboardText;
    [SerializeField] private VerticalLayoutGroup leaderboardPanel;
    [SerializeField] private TextMeshProUGUI leaderboardEntryPrefab;

    [Header("Settings")]
    [SerializeField] private int maxEntriesDisplay = 10;
    [SerializeField] private float updateInterval = 1f;

    private GameState gameState;
    private float lastUpdateTime = 0f;
    private Transform entriesContainer;

    private void Start()
    {
        Debug.Log("[LeaderboardUI] Leaderboard initialized");

        gameState = FindFirstObjectByType<GameState>();
        if (gameState == null)
        {
            Debug.LogWarning("[LeaderboardUI] GameState no encontrado!");
        }

        if (leaderboardPanel != null)
            entriesContainer = leaderboardPanel.transform;

        UpdateLeaderboard();
    }

    private void Update()
    {
        if (gameState == null)
            return;

        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateLeaderboard();
            lastUpdateTime = Time.time;
        }
    }

    // Construye el texto del leaderboard desde los datos de GameState
    private void UpdateLeaderboard()
    {
        if (gameState == null)
            return;

        var leaderboard = gameState.GetLeaderboard();

        if (leaderboardText != null)
        {
            string leaderboardContent = "=== LEADERBOARD ===\n";
            leaderboardContent += "Rank | Jugador | K | D | Racha\n";
            leaderboardContent += "========================\n";

            for (int i = 0; i < Mathf.Min(leaderboard.Count, maxEntriesDisplay); i++)
            {
                var entry = leaderboard[i];
                string playerName = GetPlayerName(entry.player);
                string rank = (i + 1).ToString();

                leaderboardContent += $"{rank.PadRight(4)} | {playerName.PadRight(8)} | " +
                    $"{entry.Item2.Kills} | {entry.Item2.Deaths} | {entry.Item2.KillStreak}\n";
            }

            leaderboardText.text = leaderboardContent;
        }
    }

    // Busca el nombre del jugador en la escena por su PlayerRef
    private string GetPlayerName(PlayerRef playerRef)
    {
        PlayerState[] allPlayers = FindObjectsByType<PlayerState>(FindObjectsSortMode.None);
        foreach (PlayerState player in allPlayers)
        {
            if (player.OwnerPlayer == playerRef)
                return player.PlayerName.ToString();
        }
        return $"Player{playerRef.PlayerId}";
    }

    // Alterna la visibilidad del leaderboard
    public void ToggleLeaderboard()
    {
        if (leaderboardText != null)
            leaderboardText.gameObject.SetActive(!leaderboardText.gameObject.activeSelf);
    }

    // Instancia singleton para acceso global
    public static LeaderboardUI Instance { get; private set; }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }
}
