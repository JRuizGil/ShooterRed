using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // Events para que LobbyUI y otros sistemas se subscriban
    public event Action<List<SessionInfo>> OnSessionListChanged;
    public event Action<PlayerRef> OnPlayerJoinedSession;
    public event Action<PlayerRef> OnPlayerLeftSession;
    public event Action<string> OnNetworkError;
    public event Action OnServerConnected;
    public event Action OnServerDisconnected;
    public event Action<ShutdownReason> OnNetworkClosed;

    private NetworkRunner currentRunner;
    private List<SessionInfo> availableSessions = new List<SessionInfo>();
    private string currentSessionName;
    private bool isHost = false;

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

    public NetworkRunner GetCurrentRunner()
    {
        return currentRunner;
    }

    public bool IsHost()
    {
        return isHost;
    }

    public string GetCurrentSessionName()
    {
        return currentSessionName;
    }

    /// <summary>
    /// Crear una nueva sala con nombre personalizado
    /// </summary>
    public async void CreateRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            OnNetworkError?.Invoke("El nombre de la sala no puede estar vacío");
            return;
        }

        currentSessionName = roomName;
        isHost = true;

        NetworkRunnerHandler handler = NetworkRunnerHandler.Instance;
        if (handler != null)
        {
            Debug.Log($"[LobbyManager] Creating room: {roomName}");
            handler.StartGame(GameMode.Shared, roomName);
        }
        else
        {
            OnNetworkError?.Invoke("NetworkRunnerHandler no encontrado");
        }
    }

    /// <summary>
    /// Unirse a una sala existente
    /// </summary>
    public void JoinRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            OnNetworkError?.Invoke("Debes seleccionar una sala");
            return;
        }

        currentSessionName = roomName;
        isHost = false;

        NetworkRunnerHandler handler = NetworkRunnerHandler.Instance;
        if (handler != null)
        {
            Debug.Log($"[LobbyManager] Joining room: {roomName}");
            handler.StartGame(GameMode.AutoHostOrClient, roomName);
        }
        else
        {
            OnNetworkError?.Invoke("NetworkRunnerHandler no encontrado");
        }
    }

    /// <summary>
    /// Obtener lista actual de sesiones disponibles
    /// </summary>
    public List<SessionInfo> GetAvailableSessions()
    {
        return new List<SessionInfo>(availableSessions);
    }

    // ===== Callbacks desde NetworkRunnerHandler =====

    /// <summary>
    /// Llamado por NetworkRunnerHandler cuando la lista de sesiones se actualiza
    /// </summary>
    public void OnSessionListUpdatedNetwork(List<SessionInfo> sessionList)
    {
        if (sessionList != null)
        {
            availableSessions = new List<SessionInfo>(sessionList);
            Debug.Log($"[LobbyManager] Sessions updated: {sessionList.Count} available");
            
            // Notificar a los listeners (UI, etc.)
            try
            {
                OnSessionListChanged?.Invoke(availableSessions);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LobbyManager] Error invoking OnSessionListChanged: {ex}");
            }
        }
    }

    /// <summary>
    /// Llamado por NetworkRunnerHandler cuando un jugador entra
    /// </summary>
    public void OnPlayerJoinedNetwork(NetworkRunner runner, PlayerRef playerRef)
    {
        if (currentRunner == null)
        {
            currentRunner = runner;
        }

        Debug.Log($"[LobbyManager] Player joined: {playerRef}");
        try
        {
            OnPlayerJoinedSession?.Invoke(playerRef);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyManager] Error invoking OnPlayerJoinedSession: {ex}");
        }
    }

    /// <summary>
    /// Llamado por NetworkRunnerHandler cuando un jugador sale
    /// </summary>
    public void OnPlayerLeftNetwork(NetworkRunner runner, PlayerRef playerRef)
    {
        Debug.Log($"[LobbyManager] Player left: {playerRef}");
        try
        {
            OnPlayerLeftSession?.Invoke(playerRef);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyManager] Error invoking OnPlayerLeftSession: {ex}");
        }
    }

    /// <summary>
    /// Llamado por NetworkRunnerHandler cuando se conecta al servidor
    /// </summary>
    public void OnConnectedToServer(NetworkRunner runner)
    {
        currentRunner = runner;
        Debug.Log($"[LobbyManager] Connected to server, runner: {runner.name}");
        try
        {
            OnServerConnected?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyManager] Error invoking OnServerConnected event: {ex}");
        }
    }

    /// <summary>
    /// Llamado por NetworkRunnerHandler cuando se desconecta
    /// </summary>
    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        Debug.Log("[LobbyManager] Disconnected from server");
        try
        {
            OnServerDisconnected?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyManager] Error invoking OnServerDisconnected event: {ex}");
        }
    }

    /// <summary>
    /// Llamado por NetworkRunnerHandler cuando hay shutdown
    /// </summary>
    public void OnNetworkShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"[LobbyManager] Network shutdown: {reason}");
        currentRunner = null;
        availableSessions.Clear();
        try
        {
            OnNetworkClosed?.Invoke(reason);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyManager] Error invoking OnNetworkClosed: {ex}");
        }
    }

    /// <summary>
    /// Desconectar de la sesión actual
    /// </summary>
    public void Disconnect()
    {
        if (currentRunner != null)
        {
            currentRunner.Shutdown();
            currentRunner = null;
        }

        isHost = false;
        currentSessionName = null;
        availableSessions.Clear();
    }
}
