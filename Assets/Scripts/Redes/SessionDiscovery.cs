using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SessionDiscovery : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;
    private static SessionDiscovery instance;

    public event Action<List<SessionInfo>> OnSessionsFound;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Initialize()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("SessionDiscovery");
            instance = obj.AddComponent<SessionDiscovery>();
            DontDestroyOnLoad(obj);
        }

        instance.StartDiscovery();
    }

    public static void Shutdown()
    {
        if (instance != null)
        {
            instance.StopDiscovery();
        }
    }

    private void StartDiscovery()
    {
        if (runner != null)
            return;

        // Create a runner just for session discovery (not playing)
        runner = gameObject.GetComponent<NetworkRunner>();
        if (runner == null)
            runner = gameObject.AddComponent<NetworkRunner>();

        runner.ProvideInput = false;
        runner.AddCallbacks(this);

        Debug.Log("[SessionDiscovery] Starting session discovery...");
    }

    private void StopDiscovery()
    {
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
            Destroy(runner);
            runner = null;
        }
    }

    #region INetworkRunnerCallbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) 
    { 
        Debug.Log("[SessionDiscovery] Connected to server");
    }
    public void OnDisconnectedFromServer(NetworkRunner runner) 
    { 
        Debug.Log("[SessionDiscovery] Disconnected from server");
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) 
    { 
        Debug.LogError($"[SessionDiscovery] Connection failed: {reason}");
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) 
    { 
        Debug.Log($"[SessionDiscovery] Sessions found: {sessionList.Count}");
        OnSessionsFound?.Invoke(sessionList);
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnSessionListUpdatedNetwork(sessionList);
        }
    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    #endregion
}
