using Fusion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estado global del juego sincronizado en red
/// Mantiene track de scores, estadísticas y estado de partida
/// </summary>
public class GameState : NetworkBehaviour
{
    [Networked] public int CurrentScoreLimit { get; set; }
    [Networked] public int MatchState { get; set; }

    // Diccionarios para estadísticas (no sincronizados directamente)
    private Dictionary<PlayerRef, PlayerStats> playerStats = new Dictionary<PlayerRef, PlayerStats>();

    // Sistema de puntuación por eventos
    [Networked] public int TotalKills { get; set; }
    [Networked] public int TotalDeaths { get; set; }

    public class PlayerStats
    {
        public int Kills = 0;
        public int Deaths = 0;
        public int KillStreak = 0;
        public int MaxKillStreak = 0;
    }

    private static GameState instance;

    public override void Spawned()
    {
        Debug.Log("[GameState] GameState spawned");
        instance = this;

        CurrentScoreLimit = 10;
        MatchState = 0;
    }

    /// <summary>
    /// Agregar un kill a un jugador
    /// </summary>
    public void AddKill(PlayerRef killer, PlayerRef victim, string cause = "Unknown")
    {
        if (!HasStateAuthority)
            return;

        // Asegurar que existen los registros
        if (!playerStats.ContainsKey(killer))
            playerStats[killer] = new PlayerStats();
        if (!playerStats.ContainsKey(victim))
            playerStats[victim] = new PlayerStats();

        // Incrementar kills y streak del atacante
        playerStats[killer].Kills++;
        playerStats[killer].KillStreak++;
        if (playerStats[killer].KillStreak > playerStats[killer].MaxKillStreak)
        {
            playerStats[killer].MaxKillStreak = playerStats[killer].KillStreak;
        }

        int killerStreak = playerStats[killer].KillStreak;

        // Resetear streak de la víctima e incrementar deaths
        playerStats[victim].Deaths++;
        playerStats[victim].KillStreak = 0;

        TotalKills++;

        // Obtener nombres de los jugadores
        PlayerState killerState = FindPlayerStateByRef(killer);
        PlayerState victimState = FindPlayerStateByRef(victim);
        string killerName = killerState != null ? killerState.PlayerName.ToString() : "Unknown";
        string victimName = victimState != null ? victimState.PlayerName.ToString() : "Unknown";

        Debug.Log($"[GameState] Kill: {killer} killed {victim}. Streak: {killerStreak}. Cause: {cause}");

        // Agregar al kill feed
        KillFeed killFeed = FindFirstObjectByType<KillFeed>();
        if (killFeed != null)
        {
            killFeed.AddKillFeedEntry(killerName, victimName, cause);
        }

        // Desbloquear habilidades por racha
        PlayerState killerPlayerState = FindPlayerStateByRef(killer);
        if (killerPlayerState != null)
        {
            PlayerHabilities abilities = killerPlayerState.GetComponent<PlayerHabilities>();
            if (abilities != null)
            {
                // Desbloquear habilidades por rachas
                if (killerStreak == 3)
                {
                    abilities.UnlockAbility(3);
                }
                else if (killerStreak == 5)
                {
                    abilities.UnlockAbility(5);
                }
                else if (killerStreak == 10)
                {
                    abilities.UnlockAbility(10);
                }
            }
        }

        // Verificar victoria
        if (playerStats[killer].Kills >= CurrentScoreLimit)
        {
            EndMatch(killer);
        }
    }

    /// <summary>
    /// Encontrar PlayerState por referencia de jugador
    /// </summary>
    private PlayerState FindPlayerStateByRef(PlayerRef playerRef)
    {
        PlayerState[] allPlayers = FindObjectsByType<PlayerState>(FindObjectsSortMode.None);
        foreach (PlayerState player in allPlayers)
        {
            if (player.OwnerPlayer == playerRef)
                return player;
        }
        return null;
    }

    /// <summary>
    /// Obtener estadísticas de un jugador
    /// </summary>
    public PlayerStats GetPlayerStats(PlayerRef player)
    {
        if (playerStats.ContainsKey(player))
            return playerStats[player];

        PlayerStats newStats = new PlayerStats();
        playerStats[player] = newStats;
        return newStats;
    }

    /// <summary>
    /// Obtener todas las estadísticas ordenadas por kills
    /// </summary>
    public List<(PlayerRef player, PlayerStats stats)> GetLeaderboard()
    {
        List<(PlayerRef, PlayerStats)> leaderboard = new List<(PlayerRef, PlayerStats)>();

        foreach (var kvp in playerStats)
        {
            leaderboard.Add((kvp.Key, kvp.Value));
        }

        // Ordenar por kills descendente
        leaderboard.Sort((a, b) => b.Item2.Kills.CompareTo(a.Item2.Kills));

        return leaderboard;
    }

    /// <summary>
    /// Finalizar la partida
    /// </summary>
    private void EndMatch(PlayerRef winner)
    {
        MatchState = 1; // 1 = Finished
        Debug.Log($"[GameState] Match ended! Winner: {winner}");

        // Aquí puedes agregar lógica de fin de juego
        // Por ejemplo: mostrar pantalla de victoria, resetear, etc.
    }

    /// <summary>
    /// Reiniciar estadísticas para nueva partida
    /// </summary>
    public void ResetStats()
    {
        if (!HasStateAuthority)
            return;

        playerStats.Clear();
        TotalKills = 0;
        TotalDeaths = 0;
        MatchState = 0;

        Debug.Log("[GameState] Stats reset for new match");
    }

    /// <summary>
    /// Singleton getter
    /// </summary>
    public static GameState Instance => instance;

    public bool CanValidateGlobalRules()
    {
        return HasStateAuthority;
    }
}
