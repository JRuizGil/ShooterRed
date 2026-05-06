using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class GameState : NetworkBehaviour
{
    [Networked] public int CurrentScoreLimit { get; set; }
    [Networked] public int MatchState        { get; set; }
    [Networked] public int TotalKills        { get; set; }
    [Networked] public int TotalDeaths       { get; set; }

    private Dictionary<PlayerRef, PlayerStats> playerStats = new Dictionary<PlayerRef, PlayerStats>();

    public class PlayerStats
    {
        public int Kills        = 0;
        public int Deaths       = 0;
        public int KillStreak   = 0;
        public int MaxKillStreak = 0;
    }

    private static GameState _instance;
    public static GameState Instance => _instance;

    public override void Spawned()
    {
        _instance = this;
        if (Object.HasStateAuthority)
        {
            CurrentScoreLimit = 10;
            MatchState        = 0;
        }
        Debug.Log("[GameState] Spawned");
    }

    public void AddKill(PlayerRef killer, PlayerRef victim, string cause = "Unknown")
    {
        if (!HasStateAuthority) return;

        if (!playerStats.ContainsKey(killer)) playerStats[killer] = new PlayerStats();
        if (!playerStats.ContainsKey(victim)) playerStats[victim] = new PlayerStats();

        playerStats[killer].Kills++;
        playerStats[killer].KillStreak++;
        if (playerStats[killer].KillStreak > playerStats[killer].MaxKillStreak)
            playerStats[killer].MaxKillStreak = playerStats[killer].KillStreak;

        int streak = playerStats[killer].KillStreak;

        playerStats[victim].Deaths++;
        playerStats[victim].KillStreak = 0;
        TotalKills++;

        // ✅ Usar PlayerRegistry en vez de FindObjectsByType
        NetworkObject killerObj = PlayerRegistry.GetPlayer(killer);
        NetworkObject victimObj = PlayerRegistry.GetPlayer(victim);

        string killerName = killerObj?.GetComponent<PlayerState>()?.PlayerName.ToString() ?? "Unknown";
        string victimName = victimObj?.GetComponent<PlayerState>()?.PlayerName.ToString() ?? "Unknown";

        Debug.Log($"[GameState] {killerName} eliminó a {victimName}. Racha: {streak}");

        KillFeed.Instance?.AddKillFeedEntry(killerName, victimName, cause);

        // Desbloquear habilidades por racha
        PlayerHabilities abilities = killerObj?.GetComponent<PlayerHabilities>();
        if (abilities != null)
        {
            if (streak == 3)  abilities.UnlockAbility(3);
            if (streak == 5)  abilities.UnlockAbility(5);
            if (streak == 10) abilities.UnlockAbility(10);
        }

        if (playerStats[killer].Kills >= CurrentScoreLimit)
            EndMatch(killer, killerName);
    }

    private void EndMatch(PlayerRef winner, string winnerName)
    {
        MatchState = 1;
        Debug.Log($"[GameState] Partida terminada! Ganador: {winnerName}");
        RPC_OnMatchEnd(winnerName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnMatchEnd(string winnerName)
    {
        Debug.Log($"[GameState] ¡{winnerName} ha ganado!");
        // Aquí mostrar pantalla de victoria
    }

    public PlayerStats GetPlayerStats(PlayerRef player)
    {
        if (!playerStats.ContainsKey(player))
            playerStats[player] = new PlayerStats();
        return playerStats[player];
    }

    public List<(PlayerRef player, PlayerStats stats)> GetLeaderboard()
    {
        var list = new List<(PlayerRef, PlayerStats)>();
        foreach (var kvp in playerStats)
            list.Add((kvp.Key, kvp.Value));
        list.Sort((a, b) => b.Item2.Kills.CompareTo(a.Item2.Kills));
        return list;
    }

    public void ResetStats()
    {
        if (!HasStateAuthority) return;
        playerStats.Clear();
        TotalKills  = 0;
        TotalDeaths = 0;
        MatchState  = 0;
    }

    public bool CanValidateGlobalRules() => HasStateAuthority;
}