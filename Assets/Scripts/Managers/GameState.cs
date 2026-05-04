using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Estructura con estadísticas de un jugador
/// </summary>
public class PlayerStats
{
    public string PlayerName;
    public int Kills;
    public int Deaths;
    public int Assists;
    public int KillStreak;
    public int MaxKillStreak;
    public int Ping;
    public int KDA => (Kills * 100) + (Assists * 50) - (Deaths * 25);
    
    public PlayerStats()
    {
        PlayerName = "Unknown";
        Kills = 0;
        Deaths = 0;
        Assists = 0;
        KillStreak = 0;
        MaxKillStreak = 0;
        Ping = 0;
    }
}

/// <summary>
/// Gestor global del estado del juego
/// Sincroniza información entre todos los clientes
/// </summary>
public class GameState : MonoBehaviour
{
    public enum GamePhase
    {
        Lobby,
        Playing,
        Paused,
        GameOver
    }
    
    [SerializeField] private GamePhase currentPhase = GamePhase.Lobby;
    
    private float gameTime = 0f;
    private int redTeamScore = 0;
    private int blueTeamScore = 0;
    
    private Dictionary<PlayerRef, PlayerState> playerStates = new();
    private Dictionary<PlayerRef, PlayerStats> playerStats = new();
    
    public static GameState Instance { get; private set; }
    
    public GamePhase CurrentPhase => currentPhase;
    public float GameTime => gameTime;
    public int RedTeamScore => redTeamScore;
    public int BlueTeamScore => blueTeamScore;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        Debug.Log("[GameState] Game state initialized");
    }
    
    private void Update()
    {
        if (currentPhase == GamePhase.Playing)
        {
            gameTime += Time.deltaTime;
        }
    }
    
    /// <summary>
    /// Registra un jugador en el GameState
    /// </summary>
    public void RegisterPlayerState(PlayerRef player, PlayerState playerState)
    {
        if (!playerStates.ContainsKey(player))
        {
            playerStates[player] = playerState;
            Debug.Log($"[GameState] Player registered: {playerState.PlayerName}");
        }
    }
    
    /// <summary>
    /// Obtiene las estadísticas de un jugador
    /// </summary>
    public PlayerStats GetPlayerStats(PlayerRef player)
    {
        if (playerStats.ContainsKey(player))
        {
            return playerStats[player];
        }
        
        // Devolver null si el jugador no existe
        return null;
    }
    
    /// <summary>
    /// Obtiene la tabla de clasificación ordenada por KDA
    /// </summary>
    public List<PlayerStats> GetLeaderboard()
    {
        return playerStats.Values
            .OrderByDescending(p => p.KDA)
            .ToList();
    }
    
    /// <summary>
    /// Agrega un kill a un jugador (atacante)
    /// </summary>
    public void AddKill(PlayerRef attacker, PlayerRef victim)
    {
        if (playerStats.ContainsKey(attacker))
        {
            var stats = playerStats[attacker];
            stats.Kills++;
            stats.KillStreak++; // Increment current kill streak
            if (stats.KillStreak > stats.MaxKillStreak) // Update max kill streak
            {
                stats.MaxKillStreak = stats.KillStreak;
            }
            playerStats[attacker] = stats;
            Debug.Log($"[GameState] {stats.PlayerName} killed an opponent! Total kills: {stats.Kills}");
        }
        
        AddDeath(victim);
    }
    
    /// <summary>
    /// Agrega una muerte a un jugador
    /// </summary>
    public void AddDeath(PlayerRef player)
    {
        if (playerStats.ContainsKey(player))
        {
            var stats = playerStats[player];
            stats.Deaths++;
            stats.KillStreak = 0; // Reset kill streak on death
            playerStats[player] = stats;
            
            Debug.Log($"[GameState] {stats.PlayerName} died! Deaths: {stats.Deaths}");
        }
    }
    
    /// <summary>
    /// Agrega un assist a un jugador
    /// </summary>
    public void AddAssist(PlayerRef player)
    {
        if (playerStats.ContainsKey(player))
        {
            var stats = playerStats[player];
            stats.Assists++;
            playerStats[player] = stats;
        }
    }
    
    public void SetGamePhase(GamePhase phase)
    {
        currentPhase = phase;
        Debug.Log($"[GameState] Game phase changed to: {phase}");
    }
    
    public void AddRedTeamScore(int points)
    {
        redTeamScore += points;
        Debug.Log($"[GameState] Red team score: {redTeamScore}");
    }
    
    public void AddBlueTeamScore(int points)
    {
        blueTeamScore += points;
        Debug.Log($"[GameState] Blue team score: {blueTeamScore}");
    }
    
    public void ResetGameState()
    {
        gameTime = 0f;
        redTeamScore = 0;
        blueTeamScore = 0;
        currentPhase = GamePhase.Lobby;
        playerStates.Clear();
        playerStats.Clear();
        Debug.Log("[GameState] Game state reset");
    }
}
