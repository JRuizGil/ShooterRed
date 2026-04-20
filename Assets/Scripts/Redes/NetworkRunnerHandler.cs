using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;
    
    public static NetworkRunnerHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public async void StartGame(GameMode mode, string sessionName)
    {
        if (runner != null)
            return;

        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError("Error: " + result.ShutdownReason);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Network] Player joined: {player}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayerJoinedNetwork(runner, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Network] Player left: {player}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayerLeftNetwork(runner, player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Network] Shutdown: {shutdownReason}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnNetworkShutdown(runner, shutdownReason);
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[Network] Connected to server");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnConnectedToServer(runner);
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        Debug.Log("[Network] Disconnected from server");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnDisconnectedFromServer(runner);
        }
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[Network] Connect failed: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[Network] Session list updated: {sessionList.Count} sessions");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnSessionListUpdatedNetwork(sessionList);
        }
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("[Network] Host migration detected");
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[Network] Scene load done");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[Network] Scene load start");
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }
}
