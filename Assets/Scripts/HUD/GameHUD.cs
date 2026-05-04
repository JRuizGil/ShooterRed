using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sistema de HUD en juego
/// Muestra vida del jugador, munición, kill streak y stats
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Health Display")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Color healthFullColor = Color.green;
    [SerializeField] private Color healthLowColor = Color.red;

    [Header("Kill Streak Display")]
    [SerializeField] private TextMeshProUGUI killStreakText;
    [SerializeField] private TextMeshProUGUI killStreakBonusText;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color crosshairNormalColor = Color.white;
    [SerializeField] private Color crosshairHitColor = Color.red;

    [Header("HUD Canvas")]
    [SerializeField] private Canvas hudCanvas;

    private PlayerHealth playerHealth;
    private GameState gameState;
    private PlayerRef localPlayer;
    private Image crosshairComponent;
    private float crosshairHitTimer = 0f;

    private void Start()
    {
        Debug.Log("[GameHUD] HUD initialized");

        // Encontrar el jugador local
        PlayerState[] allPlayers = FindObjectsByType<PlayerState>(FindObjectsSortMode.None);
        foreach (PlayerState player in allPlayers)
        {
            if (player.HasInputAuthority)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                localPlayer = player.OwnerPlayer;
                break;
            }
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("[GameHUD] No se encontró PlayerHealth local");
        }

        // Encontrar GameState
        gameState = FindFirstObjectByType<GameState>();
        if (gameState == null)
        {
            Debug.LogWarning("[GameHUD] No se encontró GameState");
        }

        // Inicializar crosshair
        if (crosshairImage != null)
        {
            crosshairComponent = crosshairImage.GetComponent<Image>();
            if (crosshairComponent != null)
                crosshairComponent.color = crosshairNormalColor;
        }

        if (hudCanvas == null)
        {
            hudCanvas = GetComponentInParent<Canvas>();
            if (hudCanvas == null)
            {
                Debug.LogWarning("[GameHUD] Canvas no encontrado");
            }
        }
    }

    private void Update()
    {
        UpdateHealthDisplay();
        UpdateKillStreakDisplay();
        UpdateStatsDisplay();
        UpdateCrosshair();
    }

    /// <summary>
    /// Actualizar display de vida
    /// </summary>
    private void UpdateHealthDisplay()
    {
        if (playerHealth == null || healthText == null)
            return;

        int hitsRemaining = playerHealth.GetHitsRemaining();
        bool isAlive = playerHealth.GetIsAlive();

        if (isAlive)
        {
            healthText.text = $"Vida: {hitsRemaining}/4";
            healthText.color = Color.white;
        }
        else
        {
            healthText.text = "MUERTO";
            healthText.color = Color.red;
        }

        if (healthBar != null)
        {
            healthBar.value = hitsRemaining / 4f;

            // Cambiar color según vida
            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(healthLowColor, healthFullColor, healthBar.value);
            }
        }
    }

    /// <summary>
    /// Actualizar display de kill streak
    /// </summary>
    private void UpdateKillStreakDisplay()
    {
        if (gameState == null || killStreakText == null)
            return;

        PlayerStats stats = gameState.GetPlayerStats(localPlayer); // Corrected scope for PlayerStats
        if (stats == null)
            return;

        killStreakText.text = $"Racha: {stats.KillStreak}";

        // Mostrar bonus especial por rachas
        if (killStreakBonusText != null)
        {
            if (stats.KillStreak >= 10)
            {
                killStreakBonusText.text = "🔥 ASESINATO EN SERIE 🔥";
                killStreakBonusText.color = new Color(1f, 0.5f, 0f); // Naranja
            }
            else if (stats.KillStreak >= 5)
            {
                killStreakBonusText.text = "⭐ RACHA INCENDIARIA ⭐";
                killStreakBonusText.color = Color.yellow;
            }
            else if (stats.KillStreak >= 3)
            {
                killStreakBonusText.text = "💥 Mata múltiple";
                killStreakBonusText.color = new Color(0f, 1f, 0.5f); // Cyan
            }
            else
            {
                killStreakBonusText.text = "";
            }
        }
    }

    /// <summary>
    /// Actualizar display de estadísticas
    /// </summary>
    private void UpdateStatsDisplay()
    {
        if (gameState == null || statsText == null)
            return;

        PlayerStats stats = gameState.GetPlayerStats(localPlayer); // Corrected scope for PlayerStats
        if (stats == null)
            return;

        statsText.text = $"K: {stats.Kills} | D: {stats.Deaths} | Max Racha: {stats.MaxKillStreak}";
    }

    /// <summary>
    /// Actualizar crosshair
    /// </summary>
    private void UpdateCrosshair()
    {
        if (crosshairComponent == null)
            return;

        // Restaurar color normal gradualmente
        if (crosshairHitTimer > 0)
        {
            crosshairHitTimer -= Time.deltaTime;
            crosshairComponent.color = crosshairHitColor;
        }
        else
        {
            crosshairComponent.color = Color.Lerp(crosshairComponent.color, crosshairNormalColor, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Indicar que el disparo acertó
    /// </summary>
    public void ShowHitFeedback()
    {
        crosshairHitTimer = 0.1f;
        if (crosshairComponent != null)
            crosshairComponent.color = crosshairHitColor;
    }

    /// <summary>
    /// Mostrar notificación de kill
    /// </summary>
    public void ShowKillNotification(string killedPlayer)
    {
        Debug.Log($"[GameHUD] Kill notification: {killedPlayer}");
        
        // Aquí puedes agregar una notificación visual flotante
        if (killStreakBonusText != null)
        {
            killStreakBonusText.text = $"¡Eliminaste a {killedPlayer}!";
            killStreakBonusText.color = Color.green;

            // Auto-hide después de 2 segundos
            Invoke(nameof(ClearKillNotification), 2f);
        }
    }

    /// <summary>
    /// Limpiar notificación de kill
    /// </summary>
    private void ClearKillNotification()
    {
        if (killStreakBonusText != null)
            killStreakBonusText.text = "";
    }

    /// <summary>
    /// Singleton getter
    /// </summary>
    public static GameHUD Instance { get; private set; }

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
