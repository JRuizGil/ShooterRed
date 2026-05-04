using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform[] spawnPoints = new Transform[4];
    
    private bool playersSpawned = false;
    private NetworkRunner networkRunner;
    
    private void Start()
    {
        networkRunner = FindObjectOfType<NetworkRunner>();
        
        if (networkRunner == null)
        {
            Debug.LogError("[PlayerSpawner] NetworkRunner not found!");
            return;
        }
        
        // Esperar a que el runner esté listo
        if (networkRunner.IsRunning)
        {
            SpawnPlayers();
        }
    }
    
    private void SpawnPlayers()
    {
        if (playersSpawned) return;
        
        if (LobbyManager.Instance == null)
        {
            Debug.LogWarning("[PlayerSpawner] LobbyManager not initialized yet, waiting...");
            Invoke("SpawnPlayers", 0.5f);
            return;
        }
        
        var playerNames = LobbyManager.Instance.GetPlayerNames();
        int spawnIndex = 0;
        
        Debug.Log($"[PlayerSpawner] Spawning {playerNames.Count} players");
        
        foreach (var playerName in playerNames)
        {
            if (spawnIndex >= spawnPoints.Length)
            {
                Debug.LogWarning("[PlayerSpawner] More players than spawn points!");
                break;
            }
            
            Vector3 spawnPos = spawnPoints[spawnIndex].position;
            Quaternion spawnRot = spawnPoints[spawnIndex].rotation;
            
            // Spawnear jugador con Fusion
            var playerInstance = networkRunner.Spawn(
                playerPrefab,
                spawnPos,
                spawnRot
            );
            
            if (playerInstance != null)
            {
                var playerMovements = playerInstance.GetComponent<PlayerMovements>();
                if (playerMovements != null)
                {
                    // Asignar nombre y equipo
                    playerMovements.SetPlayerName(playerName);
                    int teamId = spawnIndex < 2 ? 0 : 1; // Equipo 0 (Rojo) = 0-1, Equipo 1 (Azul) = 2-3
                    playerMovements.SetTeam(teamId);
                }
                
                Debug.Log($"[PlayerSpawner] Spawned player: {playerName} at position {spawnIndex} (Team: {(spawnIndex < 2 ? "Rojo" : "Azul")})");
            }
            
            spawnIndex++;
        }
        
        playersSpawned = true;
    }
    
    public void SetSpawnPoint(int index, Transform transform)
    {
        if (index >= 0 && index < spawnPoints.Length)
        {
            spawnPoints[index] = transform;
        }
    }
}
