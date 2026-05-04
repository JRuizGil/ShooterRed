using Fusion;
using UnityEngine;
using TMPro;

/// <summary>
/// Display de estadísticas del jugador
/// Muestra KDA (Kills/Deaths/Assists) en el HUD
/// </summary>
public class StatisticsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI kdaText;
    [SerializeField] private TextMeshProUGUI accuracyText;
    [SerializeField] private TextMeshProUGUI timePlayedText;

    private GameState gameState;
    private PlayerRef localPlayer;
    private int totalShots = 0;
    private int successfulShots = 0;
    private float timePlayed = 0f;

    private void Start()
    {
        gameState = FindFirstObjectByType<GameState>();
        if (gameState == null)
        {
            Debug.LogWarning("[StatisticsDisplay] GameState no encontrado");
        }

        // Encontrar jugador local
        PlayerState[] allPlayers = FindObjectsByType<PlayerState>(FindObjectsSortMode.None);
        foreach (PlayerState player in allPlayers)
        {
            if (player.HasInputAuthority)
            {
                localPlayer = player.OwnerPlayer;
                break;
            }
        }

        Debug.Log("[StatisticsDisplay] Initialized");
    }

    private void Update()
    {
        UpdateStatistics();
    }

    /// <summary>
    /// Actualizar display de estadísticas
    /// </summary>
    private void UpdateStatistics()
    {
        if (gameState == null)
            return;

        PlayerStats stats = gameState.GetPlayerStats(localPlayer); // Corrected scope for PlayerStats
        if (stats == null)
            return;

        timePlayed += Time.deltaTime;

        // Calcular K/D ratio
        float kdRatio = stats.Deaths > 0 ? (float)stats.Kills / stats.Deaths : stats.Kills;

        if (kdaText != null)
        {
            kdaText.text = $"K/D: {stats.Kills}/{stats.Deaths} ({kdRatio:F2})";
        }

        // Mostrar precisión (porcentaje de disparos acertados)
        if (accuracyText != null)
        {
            float accuracy = totalShots > 0 ? (float)successfulShots / totalShots * 100f : 0f;
            accuracyText.text = $"Precisión: {accuracy:F1}%";
        }

        // Mostrar tiempo de juego
        if (timePlayedText != null)
        {
            int minutes = (int)timePlayed / 60;
            int seconds = (int)timePlayed % 60;
            timePlayedText.text = $"Tiempo: {minutes}:{seconds:D2}";
        }
    }

    /// <summary>
    /// Registrar un disparo (llamar desde WeaponSystem)
    /// </summary>
    public void RegisterShot(bool hit)
    {
        totalShots++;
        if (hit)
            successfulShots++;
    }

    /// <summary>
    /// Resetear estadísticas
    /// </summary>
    public void ResetStatistics()
    {
        totalShots = 0;
        successfulShots = 0;
        timePlayed = 0f;
    }
}
