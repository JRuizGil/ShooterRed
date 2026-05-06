using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Health Display")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Color healthFullColor = Color.green;
    [SerializeField] private Color healthLowColor  = Color.red;

    [Header("Kill Streak")]
    [SerializeField] private TextMeshProUGUI killStreakText;
    [SerializeField] private TextMeshProUGUI killStreakBonusText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color crosshairNormalColor = Color.white;
    [SerializeField] private Color crosshairHitColor    = Color.red;

    [Header("Respawn")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TextMeshProUGUI respawnCountdownText;

    [Header("Ammo")]
    [SerializeField] private TextMeshProUGUI ammoText;

    public static GameHUD Instance { get; private set; }

    private PlayerHealth    playerHealth;
    private PlayerRef       localPlayer;
    private GameState       gameState;
    private RespawnManager  respawnManager;
    private PlayerWeaponManager weaponManager;
    private float crosshairHitTimer = 0f;
    private bool  showingDeathScreen = false;
    private float deathScreenTimer   = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Buscar jugador local con retraso para asegurar que está spawneado
        Invoke(nameof(FindLocalPlayer), 0.5f);
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    private void FindLocalPlayer()
    {
        foreach (PlayerState ps in FindObjectsByType<PlayerState>(FindObjectsSortMode.None))
        {
            if (!ps.HasInputAuthority) continue;

            playerHealth   = ps.GetComponent<PlayerHealth>();
            localPlayer    = ps.OwnerPlayer;
            respawnManager = ps.GetComponent<RespawnManager>();
            weaponManager  = ps.GetComponent<PlayerWeaponManager>();
            break;
        }

        gameState = GameState.Instance;

        if (playerHealth == null)
            Debug.LogWarning("[GameHUD] PlayerHealth local no encontrado, reintentando...");
            // Reintentar si no se encontró
            Invoke(nameof(FindLocalPlayer), 1f);
    }

    // Llamado desde PlayerState.RefreshHealthVisuals()
    public void RefreshFromPlayerState(PlayerState ps)
    {
        playerHealth  = ps.GetComponent<PlayerHealth>();
        localPlayer   = ps.OwnerPlayer;
        respawnManager = ps.GetComponent<RespawnManager>();
        weaponManager  = ps.GetComponent<PlayerWeaponManager>();
        gameState      = GameState.Instance;
    }

    private void Update()
    {
        UpdateHealthDisplay();
        UpdateKillStreakDisplay();
        UpdateStatsDisplay();
        UpdateCrosshair();
        UpdateAmmoDisplay();
        UpdateRespawnCountdown();
    }

    private void UpdateHealthDisplay()
    {
        if (playerHealth == null || healthText == null) return;

        int  hits    = playerHealth.GetHitsRemaining();
        bool isAlive = playerHealth.GetIsAlive();

        healthText.text  = isAlive ? $"Vida: {hits}/4" : "MUERTO";
        healthText.color = isAlive ? Color.white : Color.red;

        if (healthBar != null)
        {
            healthBar.value = hits / 4f;
            Image fill = healthBar.fillRect?.GetComponent<Image>();
            if (fill != null)
                fill.color = Color.Lerp(healthLowColor, healthFullColor, healthBar.value);
        }
    }

    private void UpdateKillStreakDisplay()
    {
        if (gameState == null || killStreakText == null) return;

        GameState.PlayerStats stats = gameState.GetPlayerStats(localPlayer);
        if (stats == null) return;

        killStreakText.text = $"Racha: {stats.KillStreak}";

        if (killStreakBonusText != null)
        {
            if      (stats.KillStreak >= 10) { killStreakBonusText.text = "🔥 ASESINATO EN SERIE"; killStreakBonusText.color = new Color(1f,0.5f,0f); }
            else if (stats.KillStreak >= 5)  { killStreakBonusText.text = "⭐ RACHA INCENDIARIA";  killStreakBonusText.color = Color.yellow; }
            else if (stats.KillStreak >= 3)  { killStreakBonusText.text = "💥 Mata múltiple";      killStreakBonusText.color = new Color(0f,1f,0.5f); }
            else                              { killStreakBonusText.text = ""; }
        }
    }

    private void UpdateStatsDisplay()
    {
        if (gameState == null || statsText == null) return;
        GameState.PlayerStats stats = gameState.GetPlayerStats(localPlayer);
        if (stats == null) return;
        statsText.text = $"K: {stats.Kills} | D: {stats.Deaths} | Max: {stats.MaxKillStreak}";
    }

    private void UpdateAmmoDisplay()
    {
        if (weaponManager == null || ammoText == null) return;
        BaseWeapon weapon = weaponManager.GetCurrentWeapon();
        ammoText.text = weapon != null ? weapon.GetAmmoDisplay() : "--/--";
    }

    private void UpdateCrosshair()
    {
        if (crosshairImage == null) return;
        if (crosshairHitTimer > 0)
        {
            crosshairHitTimer -= Time.deltaTime;
            crosshairImage.color = crosshairHitColor;
        }
        else
        {
            crosshairImage.color = Color.Lerp(crosshairImage.color, crosshairNormalColor, Time.deltaTime * 5f);
        }
    }

    private void UpdateRespawnCountdown()
    {
        if (!showingDeathScreen || respawnManager == null) return;

        float countdown = respawnManager.GetRespawnCountdown();
        if (respawnCountdownText != null)
            respawnCountdownText.text = $"Respawn en {countdown:F1}s";
    }

    // =========================================================
    // API PÚBLICA
    // =========================================================

    public void ShowHitFeedback()
    {
        crosshairHitTimer    = 0.1f;
        crosshairImage.color = crosshairHitColor;
    }

    public void ShowDeathScreen(float respawnDelay)
    {
        showingDeathScreen = true;
        deathScreenTimer   = respawnDelay;
        if (deathPanel != null) deathPanel.SetActive(true);
    }

    public void HideDeathScreen()
    {
        showingDeathScreen = false;
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    public void ShowKillNotification(string killedPlayer)
    {
        if (killStreakBonusText == null) return;
        killStreakBonusText.text  = $"¡Eliminaste a {killedPlayer}!";
        killStreakBonusText.color = Color.green;
        Invoke(nameof(ClearKillNotification), 2f);
    }

    private void ClearKillNotification()
    {
        if (killStreakBonusText != null) killStreakBonusText.text = "";
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}