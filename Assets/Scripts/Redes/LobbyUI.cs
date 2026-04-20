using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Fusion;
using System.Linq;

public class LobbyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform sessionListContainer; // Contenedor para items de lista
    [SerializeField] private Prefab sessionListItemPrefab; // Prefab para cada item de la lista
    [SerializeField] private Text sessionCountText; // Mostrar cantidad de sesiones
    [SerializeField] private Text playerCountText; // Mostrar jugadores en sesión actual
    [SerializeField] private Button refreshSessionsButton;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject lobbyPanel; // Panel del lobby que se activa/desactiva

    private List<GameObject> sessionListItems = new List<GameObject>();
    private NetworkRunner currentRunner;

    private void Start()
    {
        // Suscribirse a eventos del LobbyManager
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnSessionListChanged += RefreshSessionList;
            LobbyManager.Instance.OnPlayerJoinedSession += OnPlayerJoined;
            LobbyManager.Instance.OnPlayerLeftSession += OnPlayerLeft;
            LobbyManager.Instance.OnServerConnected += OnConnectedToServer;
            LobbyManager.Instance.OnServerDisconnected += OnDisconnectedFromServer;
            LobbyManager.Instance.OnNetworkError += OnNetworkError;
        }

        // Conectar botones
        if (refreshSessionsButton != null)
        {
            refreshSessionsButton.onClick.AddListener(RefreshSessionListManual);
        }

        if (startMatchButton != null)
        {
            startMatchButton.onClick.AddListener(OnStartMatchButton);
        }

        // Estado inicial
        UpdateStatus("En espera de conexión");
    }

    private void OnDestroy()
    {
        // Desuscribirse de eventos
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnSessionListChanged -= RefreshSessionList;
            LobbyManager.Instance.OnPlayerJoinedSession -= OnPlayerJoined;
            LobbyManager.Instance.OnPlayerLeftSession -= OnPlayerLeft;
            LobbyManager.Instance.OnServerConnected -= OnConnectedToServer;
            LobbyManager.Instance.OnServerDisconnected -= OnDisconnectedFromServer;
            LobbyManager.Instance.OnNetworkError -= OnNetworkError;
        }

        if (refreshSessionsButton != null)
        {
            refreshSessionsButton.onClick.RemoveListener(RefreshSessionListManual);
        }

        if (startMatchButton != null)
        {
            startMatchButton.onClick.RemoveListener(OnStartMatchButton);
        }
    }

    /// <summary>
    /// Actualizar lista de sesiones disponibles
    /// </summary>
    private void RefreshSessionList(List<SessionInfo> sessions)
    {
        Debug.Log($"[LobbyUI] Refreshing session list: {sessions.Count} sessions");

        // Limpiar lista anterior
        foreach (GameObject item in sessionListItems)
        {
            Destroy(item);
        }
        sessionListItems.Clear();

        // Crear items para cada sesión
        foreach (SessionInfo session in sessions)
        {
            CreateSessionListItem(session);
        }

        // Actualizar contador
        if (sessionCountText != null)
        {
            sessionCountText.text = $"Salas disponibles: {sessions.Count}";
        }

        UpdateStatus($"{sessions.Count} sala(s) disponible(s)");
    }

    /// <summary>
    /// Crear un item de la lista para una sesión
    /// </summary>
    private void CreateSessionListItem(SessionInfo session)
    {
        if (sessionListContainer == null)
        {
            Debug.LogWarning("[LobbyUI] Session list container not assigned");
            return;
        }

        // Crear item (por ahora, un botón simple con text)
        // En un proyecto real, usarías un prefab instanciable
        GameObject itemObj = new GameObject($"SessionItem_{session.Name}");
        itemObj.transform.SetParent(sessionListContainer, false);

        // Crear layout
        LayoutElement layout = itemObj.AddComponent<LayoutElement>();
        layout.preferredHeight = 50;

        // Crear botón
        Button btn = itemObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.gray;
        colors.highlightedColor = Color.white;
        btn.colors = colors;

        // Crear text para mostrar nombre y jugadores
        Text btnText = itemObj.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        btnText.text = $"{session.Name} ({session.PlayerCount})";
        btnText.color = Color.white;

        // Conectar click al método de unirse
        SessionInfo sessionCopy = session; // Captura para closure
        btn.onClick.AddListener(() => OnSessionItemClicked(sessionCopy));

        sessionListItems.Add(itemObj);
    }

    /// <summary>
    /// Llamado cuando clickean en una sesión de la lista
    /// </summary>
    private void OnSessionItemClicked(SessionInfo session)
    {
        Debug.Log($"[LobbyUI] Joining session: {session.Name}");
        
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.JoinRoom(session.Name);
        }

        UpdateStatus($"Uniéndote a {session.Name}...");
    }

    /// <summary>
    /// Recargar lista de sesiones manualmente
    /// </summary>
    private void RefreshSessionListManual()
    {
        Debug.Log("[LobbyUI] Manual refresh sessions");
        
        if (LobbyManager.Instance != null)
        {
            // Fusion actualiza la lista periódicamente, pero puedes forzar aquí si needed
            var sessions = LobbyManager.Instance.GetAvailableSessions();
            RefreshSessionList(sessions);
        }
    }

    /// <summary>
    /// Actualizar cuando jugador entra
    /// </summary>
    private void OnPlayerJoined(PlayerRef player)
    {
        Debug.Log($"[LobbyUI] Player joined: {player}");
        UpdatePlayerCount();
        UpdateStatus($"Jugador {player} se unió");
    }

    /// <summary>
    /// Actualizar cuando jugador sale
    /// </summary>
    private void OnPlayerLeft(PlayerRef player)
    {
        Debug.Log($"[LobbyUI] Player left: {player}");
        UpdatePlayerCount();
        UpdateStatus($"Jugador {player} salió");
    }

    /// <summary>
    /// Actualizar contador de jugadores
    /// </summary>
    private void UpdatePlayerCount()
    {
        currentRunner = LobbyManager.Instance?.GetCurrentRunner();
        if (currentRunner != null && playerCountText != null)
        {
            int playerCount = currentRunner.ActivePlayers.Count();
            playerCountText.text = $"Jugadores en sala: {playerCount}";
        }
    }

    /// <summary>
    /// Conectado al servidor
    /// </summary>
    private void OnConnectedToServer()
    {
        Debug.Log("[LobbyUI] Connected to server");
        currentRunner = LobbyManager.Instance?.GetCurrentRunner();
        UpdatePlayerCount();
        UpdateStatus("Conectado al servidor");

        // Mostrar botón de iniciar si eres host
        if (startMatchButton != null && LobbyManager.Instance != null)
        {
            startMatchButton.interactable = LobbyManager.Instance.IsHost();
        }
    }

    /// <summary>
    /// Desconectado del servidor
    /// </summary>
    private void OnDisconnectedFromServer()
    {
        Debug.Log("[LobbyUI] Disconnected from server");
        currentRunner = null;
        sessionListItems.Clear();
        UpdateStatus("Desconectado");

        if (startMatchButton != null)
        {
            startMatchButton.interactable = false;
        }
    }

    /// <summary>
    /// Error en la red
    /// </summary>
    private void OnNetworkError(string errorMessage)
    {
        Debug.LogError($"[LobbyUI] Network error: {errorMessage}");
        UpdateStatus($"Error: {errorMessage}");
    }

    /// <summary>
    /// Iniciar la partida (solo host)
    /// </summary>
    private void OnStartMatchButton()
    {
        if (LobbyManager.Instance == null || !LobbyManager.Instance.IsHost())
        {
            Debug.LogWarning("[LobbyUI] Only host can start match");
            return;
        }

        Debug.Log("[LobbyUI] Starting match");
        UpdateStatus("¡Partida iniciada!");

        // Aquí podrías desactivar el panel del lobby
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }

        // Aquí irían eventos/callbacks para iniciar la partida (loading, countdown, etc.)
    }

    /// <summary>
    /// Actualizar texto de estado
    /// </summary>
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}

/// <summary>
/// Serializable para prefabs de UI items
/// (En un proyecto real, tendrías un prefab en Assets)
/// </summary>
[System.Serializable]
public struct Prefab
{
    public GameObject prefab;
}
