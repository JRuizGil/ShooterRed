using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // Eventos para la UI
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
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Al arrancar el LobbyManager, conectarse al lobby de Fusion para
        // recibir la lista de sesiones disponibles automáticamente.
        ConnectToFusionLobby();
    }

    // =====================================================================
    // CONECTAR AL LOBBY DE FUSION (descubrimiento de sesiones)
    // Esto NO inicia una partida, solo conecta al matchmaking para ver salas.
    // =====================================================================
    public void ConnectToFusionLobby()
    {
        NetworkRunnerHandler handler = NetworkRunnerHandler.Instance;
        if (handler != null)
        {
            Debug.Log("[LobbyManager] Conectando al lobby de Fusion...");
            handler.ConnectToLobby();
        }
        else
        {
            Debug.LogError("[LobbyManager] NetworkRunnerHandler no encontrado");
            OnNetworkError?.Invoke("NetworkRunnerHandler no encontrado");
        }
    }

    public NetworkRunner GetCurrentRunner() => currentRunner;
    public bool IsHost() => isHost;
    public string GetCurrentSessionName() => currentSessionName;

    // =====================================================================
    // CREAR SALA — registra la sesión en Fusion inmediatamente en modo Host
    // SIN cargar escena de juego. La sala aparece visible para otros clientes.
    // El host pulsa "Iniciar Partida" cuando quiere cargar el nivel.
    // =====================================================================
    public void CreateRoom(string roomName)
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
            Debug.Log($"[LobbyManager] Creando sala en Fusion: {roomName}");
            // Crear sesión en Fusion SIN escena de juego (scene = -1 / default).
            // Esto publica la sala en el matchmaking para que otros la vean.
            handler.CreateLobbySession(roomName);
        }
        else
        {
            OnNetworkError?.Invoke("NetworkRunnerHandler no encontrado");
        }
    }

    // =====================================================================
    // UNIRSE A SALA — el cliente se une a una sesión existente.
    // =====================================================================
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
            Debug.Log($"[LobbyManager] Uniéndose a sala: {roomName}");
            handler.JoinLobbySession(roomName);
        }
        else
        {
            OnNetworkError?.Invoke("NetworkRunnerHandler no encontrado");
        }
    }

    // =====================================================================
    // INICIAR PARTIDA — solo el host. Carga la escena de juego.
    // =====================================================================
    public void StartMatch()
    {
        if (!isHost)
        {
            Debug.LogWarning("[LobbyManager] Solo el host puede iniciar la partida");
            return;
        }

        if (string.IsNullOrEmpty(currentSessionName))
        {
            OnNetworkError?.Invoke("No hay sala activa");
            return;
        }

        NetworkRunnerHandler handler = NetworkRunnerHandler.Instance;
        if (handler != null)
        {
            Debug.Log($"[LobbyManager] Cargando escena de juego: {currentSessionName}");
            handler.LoadGameScene();
        }
        else
        {
            OnNetworkError?.Invoke("NetworkRunnerHandler no encontrado");
        }
    }

    public List<SessionInfo> GetAvailableSessions() => new List<SessionInfo>(availableSessions);

    // =====================================================================
    // Callbacks desde NetworkRunnerHandler
    // =====================================================================

    public void OnSessionListUpdatedNetwork(List<SessionInfo> sessionList)
    {
        if (sessionList == null) return;

        availableSessions = new List<SessionInfo>(sessionList);
        Debug.Log($"[LobbyManager] Lista de sesiones actualizada: {sessionList.Count} sesiones");

        try { OnSessionListChanged?.Invoke(availableSessions); }
        catch (Exception ex) { Debug.LogError($"[LobbyManager] Error en OnSessionListChanged: {ex}"); }
    }

    public void OnPlayerJoinedNetwork(NetworkRunner runner, PlayerRef playerRef)
    {
        if (currentRunner == null) currentRunner = runner;

        Debug.Log($"[LobbyManager] Jugador entró: {playerRef}");
        try { OnPlayerJoinedSession?.Invoke(playerRef); }
        catch (Exception ex) { Debug.LogError($"[LobbyManager] Error en OnPlayerJoinedSession: {ex}"); }
    }

    public void OnPlayerLeftNetwork(NetworkRunner runner, PlayerRef playerRef)
    {
        Debug.Log($"[LobbyManager] Jugador salió: {playerRef}");
        try { OnPlayerLeftSession?.Invoke(playerRef); }
        catch (Exception ex) { Debug.LogError($"[LobbyManager] Error en OnPlayerLeftSession: {ex}"); }
    }

    public void NotifyConnectedToServer(NetworkRunner runner)
    {
        currentRunner = runner;
        Debug.Log($"[LobbyManager] Conectado al servidor: {runner.name}");
        try { OnServerConnected?.Invoke(); }
        catch (Exception ex) { Debug.LogError($"[LobbyManager] Error en OnServerConnected: {ex}"); }
    }

    public void NotifyDisconnectedFromServer(NetworkRunner runner)
    {
        Debug.Log("[LobbyManager] Desconectado del servidor");
        try { OnServerDisconnected?.Invoke(); }
        catch (Exception ex) { Debug.LogError($"[LobbyManager] Error en OnServerDisconnected: {ex}"); }
    }

    public void NotifyNetworkError(string error)
    {
        Debug.LogError($"[LobbyManager] Error de red: {error}");
        try { OnNetworkError?.Invoke(error); }
        catch (Exception ex) { Debug.LogError($"[LobbyManager] Error en OnNetworkError: {ex}"); }
    }

    public void OnNetworkShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"[LobbyManager] Network shutdown: {reason}");
        currentRunner = null;
        availableSessions.Clear();
        try { OnNetworkClosed?.Invoke(reason); }
        catch (Exception ex) { Debug.LogError($"[LobbyManager] Error en OnNetworkClosed: {ex}"); }
    }

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