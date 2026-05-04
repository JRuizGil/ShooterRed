using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manejador de spawn de players en red.
/// Se encarga de instanciar players cuando se conectan a través de Fusion Network.
/// </summary>
public class NetworkPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private SpawnPointManager spawnPointManager;

    private NetworkRunner runner;

    private void Start()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        
        if (spawnPointManager == null)
            spawnPointManager = FindFirstObjectByType<SpawnPointManager>();

        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] Player prefab no asignado!");
        }

        if (runner != null)
        {
            runner.AddCallbacks(this);
        }
        else
        {
            Debug.LogWarning("[NetworkPlayerSpawner] NetworkRunner no encontrado en la escena");
        }
    }

    /// <summary>
    /// Spawnear un jugador en la red
    /// </summary>
    public void SpawnPlayer(NetworkRunner runner, PlayerRef playerRef)
    {
        if (runner == null || playerPrefab == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] Runner o playerPrefab es null");
            return;
        }

        // Obtener punto de spawn
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (spawnPointManager != null)
        {
            (Vector3 position, Quaternion rotation) spawnPoint = spawnPointManager.GetNextSpawnPoint();
            spawnPos = spawnPoint.position;
            spawnRot = spawnPoint.rotation;
        }

        // Spawnear player en la red
        var playerObject = runner.Spawn(
            playerPrefab,
            spawnPos,
            spawnRot,
            playerRef
        );

        Debug.Log($"[NetworkPlayerSpawner] Player {playerRef} spawnado en {spawnPos}");
    }

    // ===== Implementación de INetworkRunnerCallbacks =====

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkPlayerSpawner] Jugador unido: {player}");
        SpawnPlayer(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkPlayerSpawner] Jugador se fue: {player}");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
