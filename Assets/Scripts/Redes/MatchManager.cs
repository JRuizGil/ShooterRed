using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestor de partida
/// Controla el ciclo de vida completo de una partida (inicio, gameplay, fin)
/// </summary>
public class MatchManager : NetworkBehaviour
{
    [Header("Match Settings")]
    [SerializeField] private int killsToWin = 10;
    [SerializeField] private float matchDuration = 300f; // 5 minutos
    [SerializeField] private float preparationTime = 5f; // Tiempo antes de empezar

    [Header("References")]
    [SerializeField] private SpawnPointManager spawnPointManager;

    // Estados de partida
    public enum MatchState { Preparation, Playing, Finished }

    [Networked] public MatchState CurrentMatchState { get; set; } = MatchState.Preparation;
    [Networked] public float MatchTimeRemaining { get; set; }
    [Networked] public NetworkObject WinnerPlayer { get; set; }

    private GameState gameState;
    private float preparationTimer = 0f;
    private bool matchStarted = false;
    private static MatchManager instance;

    public override void Spawned()
    {
        Debug.Log("[MatchManager] Match manager spawned");
        instance = this;

        gameState = GetComponent<GameState>();
        if (gameState == null)
            gameState = FindFirstObjectByType<GameState>();

        if (spawnPointManager == null)
            spawnPointManager = FindFirstObjectByType<SpawnPointManager>();

        MatchTimeRemaining = matchDuration;
        preparationTimer = preparationTime;

        // Sincronizar configuración en GameState
        if (gameState != null)
        {
            gameState.CurrentScoreLimit = killsToWin;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        switch (CurrentMatchState)
        {
            case MatchState.Preparation:
                UpdatePreparation();
                break;
            case MatchState.Playing:
                UpdatePlaying();
                break;
            case MatchState.Finished:
                UpdateFinished();
                break;
        }
    }

    /// <summary>
    /// Actualizar fase de preparación
    /// </summary>
    private void UpdatePreparation()
    {
        preparationTimer -= Runner.DeltaTime;

        if (preparationTimer <= 0)
        {
            StartMatch();
        }
    }

    /// <summary>
    /// Actualizar fase de juego
    /// </summary>
    private void UpdatePlaying()
    {
        MatchTimeRemaining -= Runner.DeltaTime;

        // Verificar victoria por tiempo
        if (MatchTimeRemaining <= 0)
        {
            EndMatch();
        }

        // Verificar victoria por kills
        if (gameState != null)
        {
            var leaderboard = gameState.GetLeaderboard();
            if (leaderboard.Count > 0 && leaderboard[0].stats.Kills >= killsToWin)
            {
                EndMatch();
            }
        }
    }

    /// <summary>
    /// Actualizar fase de fin de partida
    /// </summary>
    private void UpdateFinished()
    {
        // Esperar antes de volver al lobby
        // Aquí puedes agregar temporizador para volver automáticamente
        Debug.Log("[MatchManager] Match finished. Waiting for players to return to lobby.");
    }

    /// <summary>
    /// Iniciar la partida
    /// </summary>
    private void StartMatch()
    {
        CurrentMatchState = MatchState.Playing;
        MatchTimeRemaining = matchDuration;
        matchStarted = true;

        Debug.Log("[MatchManager] MATCH STARTED!");

        // Descongelar jugadores
        RPC_UnfreezeAllPlayers();
    }

    /// <summary>
    /// Finalizar la partida
    /// </summary>
    private void EndMatch()
    {
        CurrentMatchState = MatchState.Finished;
        matchStarted = false;

        Debug.Log("[MatchManager] MATCH ENDED!");

        // Mostrar pantalla de fin de juego
        RPC_ShowEndGameScreen();
    }

    /// <summary>
    /// Reiniciar partida
    /// </summary>
    public void RestartMatch()
    {
        if (!HasStateAuthority)
            return;

        CurrentMatchState = MatchState.Preparation;
        MatchTimeRemaining = matchDuration;
        preparationTimer = preparationTime;
        matchStarted = false;

        if (gameState != null)
        {
            gameState.ResetStats();
        }

        if (spawnPointManager != null)
        {
            spawnPointManager.ResetSpawnIndex();
        }

        Debug.Log("[MatchManager] Match restarted");
    }

    /// <summary>
    /// RPC para descongelar jugadores
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UnfreezeAllPlayers()
    {
        PlayerMovements[] players = FindObjectsByType<PlayerMovements>(FindObjectsSortMode.None);
        foreach (PlayerMovements player in players)
        {
            player.enabled = true;
        }

        Debug.Log("[MatchManager] All players unfrozen");
    }

    /// <summary>
    /// RPC para mostrar pantalla de fin de juego
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowEndGameScreen()
    {
        // Aquí puedes agregar lógica para mostrar UI de fin de juego
        Debug.Log("[MatchManager] End game screen shown");
    }

    /// <summary>
    /// Getter para saber si la partida empezó
    /// </summary>
    public bool IsMatchStarted()
    {
        return matchStarted && CurrentMatchState == MatchState.Playing;
    }

    /// <summary>
    /// Getter para estado actual
    /// </summary>
    public MatchState GetCurrentState()
    {
        return CurrentMatchState;
    }

    /// <summary>
    /// Singleton getter
    /// </summary>
    public static MatchManager Instance => instance;
}
