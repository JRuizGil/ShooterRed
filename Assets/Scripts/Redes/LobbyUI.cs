using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Fusion;
using System.Linq;
using TMPro;

public class LobbyUIFusion : MonoBehaviour
{
    [Header("Paneles principales")]
    [SerializeField] private GameObject mainMenuPanel;      // Panel con botones Crear/Unirse
    [SerializeField] private GameObject lobbyWaitPanel;     // Panel de espera tras crear sala (solo host)
    [SerializeField] private GameObject sessionListPanel;   // Panel con lista de salas disponibles

    [Header("Crear sala")]
    [SerializeField] private TMP_InputField createRoomInput;
    [SerializeField] private Button createRoomButton;

    [Header("Lista de sesiones")]
    [SerializeField] private Transform sessionListContainer;    // Scroll View > Viewport > Content
    [SerializeField] private GameObject sessionListItemPrefab;  // Prefab: botón con TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI sessionCountText;
    [SerializeField] private Button refreshSessionsButton;
    [SerializeField] private Button showSessionListButton;      // Botón "Ver salas disponibles"

    [Header("Panel de espera (host)")]
    [SerializeField] private TextMeshProUGUI waitingRoomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button startMatchButton;           // Solo visible/activo para el host
    [SerializeField] private Button cancelRoomButton;

    [Header("Estado")]
    [SerializeField] private TextMeshProUGUI statusText;

    private List<GameObject> sessionListItems = new List<GameObject>();

    // =====================================================================
    private void Start()
    {
        SubscribeToLobbyEvents();
        SetupButtons();

        // Mostrar el menú principal al inicio
        ShowMainMenu();
        UpdateStatus("Conectando al lobby...");
    }

    private void OnDestroy()
    {
        UnsubscribeFromLobbyEvents();
    }

    // =====================================================================
    // SUSCRIPCIÓN DE EVENTOS
    // =====================================================================

    private void SubscribeToLobbyEvents()
    {
        if (LobbyManager.Instance == null) return;
        LobbyManager.Instance.OnSessionListChanged  += OnSessionListReceived;
        LobbyManager.Instance.OnPlayerJoinedSession += OnPlayerJoined;
        LobbyManager.Instance.OnPlayerLeftSession   += OnPlayerLeft;
        LobbyManager.Instance.OnServerConnected     += OnConnectedToServer;
        LobbyManager.Instance.OnServerDisconnected  += OnDisconnectedFromServer;
        LobbyManager.Instance.OnNetworkError        += OnNetworkError;
    }

    private void UnsubscribeFromLobbyEvents()
    {
        if (LobbyManager.Instance == null) return;
        LobbyManager.Instance.OnSessionListChanged  -= OnSessionListReceived;
        LobbyManager.Instance.OnPlayerJoinedSession -= OnPlayerJoined;
        LobbyManager.Instance.OnPlayerLeftSession   -= OnPlayerLeft;
        LobbyManager.Instance.OnServerConnected     -= OnConnectedToServer;
        LobbyManager.Instance.OnServerDisconnected  -= OnDisconnectedFromServer;
        LobbyManager.Instance.OnNetworkError        -= OnNetworkError;
    }

    // =====================================================================
    // SETUP DE BOTONES
    // =====================================================================

    private void SetupButtons()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);

        if (refreshSessionsButton != null)
            refreshSessionsButton.onClick.AddListener(OnRefreshClicked);

        if (showSessionListButton != null)
            showSessionListButton.onClick.AddListener(ShowSessionListPanel);

        if (startMatchButton != null)
            startMatchButton.onClick.AddListener(OnStartMatchClicked);

        if (cancelRoomButton != null)
            cancelRoomButton.onClick.AddListener(OnCancelRoomClicked);
    }

    // =====================================================================
    // NAVEGACIÓN DE PANELES
    // =====================================================================

    private void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(lobbyWaitPanel, false);
        SetPanelActive(sessionListPanel, false);
    }

    private void ShowLobbyWaitPanel(string roomName)
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(lobbyWaitPanel, true);
        SetPanelActive(sessionListPanel, false);

        if (waitingRoomNameText != null)
            waitingRoomNameText.text = $"Sala: {roomName}";

        // El botón de iniciar solo es interactivo para el host
        if (startMatchButton != null)
            startMatchButton.interactable = LobbyManager.Instance != null && LobbyManager.Instance.IsHost();
    }

    private void ShowSessionListPanel()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(lobbyWaitPanel, false);
        SetPanelActive(sessionListPanel, true);

        // Refrescar la lista al mostrar el panel
        OnRefreshClicked();
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    // =====================================================================
    // BOTÓN: CREAR SALA
    // =====================================================================

    private void OnCreateRoomClicked()
    {
        if (createRoomInput == null || string.IsNullOrEmpty(createRoomInput.text))
        {
            UpdateStatus("Escribe un nombre para la sala");
            return;
        }

        string roomName = createRoomInput.text.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            UpdateStatus("El nombre de la sala no puede estar vacío");
            return;
        }

        Debug.Log($"[LobbyUI] Creando sala: {roomName}");
        LobbyManager.Instance?.CreateRoom(roomName);

        // Ir al panel de espera del host
        ShowLobbyWaitPanel(roomName);
        UpdateStatus($"Sala \"{roomName}\" creada. Esperando jugadores...");
    }

    // =====================================================================
    // BOTÓN: REFRESCAR LISTA DE SESIONES
    // =====================================================================

    private void OnRefreshClicked()
    {
        Debug.Log("[LobbyUI] Refrescando lista de sesiones");

        if (LobbyManager.Instance != null)
        {
            // Obtener la lista ya cacheada en LobbyManager
            var sessions = LobbyManager.Instance.GetAvailableSessions();
            RebuildSessionList(sessions);

            // Además reconectar al lobby de Fusion para forzar actualización
            LobbyManager.Instance.ConnectToFusionLobby();
        }

        UpdateStatus("Buscando salas...");
    }

    // =====================================================================
    // LISTA DE SESIONES
    // =====================================================================

    // Llamado automáticamente por el evento de LobbyManager cuando Fusion
    // actualiza la lista (se llama periódicamente sin necesidad de polling).
    private void OnSessionListReceived(List<SessionInfo> sessions)
    {
        Debug.Log($"[LobbyUI] Sesiones recibidas: {sessions.Count}");
        RebuildSessionList(sessions);
        UpdateStatus($"{sessions.Count} sala(s) disponible(s)");
    }

    private void RebuildSessionList(List<SessionInfo> sessions)
    {
        // Limpiar items anteriores
        foreach (GameObject item in sessionListItems)
            Destroy(item);
        sessionListItems.Clear();

        if (sessionCountText != null)
            sessionCountText.text = $"Salas disponibles: {sessions.Count}";

        if (sessions.Count == 0)
        {
            SpawnSessionItem("(No hay salas disponibles)", default, false);
            return;
        }

        foreach (SessionInfo session in sessions)
            SpawnSessionItem($"{session.Name}  [{session.PlayerCount}/{session.MaxPlayers}]", session, true);
    }

    // Crea un botón por cada sesión en el scroll view
    // isClickable = false para el item "no hay salas"
    private void SpawnSessionItem(string label, SessionInfo session, bool isClickable)
    {
        if (sessionListContainer == null)
        {
            Debug.LogError("[LobbyUI] sessionListContainer no asignado en el Inspector");
            return;
        }

        GameObject itemObj;

        if (sessionListItemPrefab != null)
        {
            itemObj = Instantiate(sessionListItemPrefab, sessionListContainer);
        }
        else
        {
            itemObj = CreateDynamicSessionButton();
        }

        itemObj.name = $"SessionItem_{label}";

        TextMeshProUGUI tmp = itemObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = label;

        Button btn = itemObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = isClickable;

            if (isClickable)
            {
                SessionInfo captured = session;
                btn.onClick.AddListener(() => OnSessionItemClicked(captured));
            }
        }

        sessionListItems.Add(itemObj);
    }

    private GameObject CreateDynamicSessionButton()
    {
        GameObject obj = new GameObject("SessionBtn");
        obj.transform.SetParent(sessionListContainer, false);

        // RectTransform
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 50);

        // Fondo
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // Botón
        Button btn = obj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        colors.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = colors;

        // Layout
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = 50;
        layout.flexibleWidth   = 1;

        // Texto
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 0);
        textRt.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.color     = Color.white;
        tmp.fontSize  = 16;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        return obj;
    }

    // =====================================================================
    // CLICK EN SESIÓN → UNIRSE
    // =====================================================================

    private void OnSessionItemClicked(SessionInfo session)
    {
        Debug.Log($"[LobbyUI] Uniéndose a: {session.Name}");
        UpdateStatus($"Uniéndote a \"{session.Name}\"...");

        LobbyManager.Instance?.JoinRoom(session.Name);
        ShowLobbyWaitPanel(session.Name);
    }

    // =====================================================================
    // BOTÓN: INICIAR PARTIDA (solo host)
    // =====================================================================

    private void OnStartMatchClicked()
    {
        if (LobbyManager.Instance == null || !LobbyManager.Instance.IsHost())
        {
            UpdateStatus("Solo el host puede iniciar la partida");
            return;
        }

        Debug.Log("[LobbyUI] Host iniciando partida...");
        UpdateStatus("Iniciando partida...");
        LobbyManager.Instance.StartMatch();
    }

    // =====================================================================
    // BOTÓN: CANCELAR SALA
    // =====================================================================

    private void OnCancelRoomClicked()
    {
        LobbyManager.Instance?.Disconnect();
        ShowMainMenu();
        UpdateStatus("Sala cancelada");

        // Reconectar al lobby para seguir viendo sesiones
        LobbyManager.Instance?.ConnectToFusionLobby();
    }

    // =====================================================================
    // EVENTOS DE RED
    // =====================================================================

    private void OnPlayerJoined(PlayerRef player)
    {
        Debug.Log($"[LobbyUI] Jugador entró: {player}");
        UpdatePlayerCount();
    }

    private void OnPlayerLeft(PlayerRef player)
    {
        Debug.Log($"[LobbyUI] Jugador salió: {player}");
        UpdatePlayerCount();
    }

    private void UpdatePlayerCount()
    {
        var runner = LobbyManager.Instance?.GetCurrentRunner();
        if (runner != null && playerCountText != null)
        {
            int count = runner.ActivePlayers.Count();
            playerCountText.text = $"Jugadores: {count}";
        }
    }

    private void OnConnectedToServer()
    {
        Debug.Log("[LobbyUI] Conectado al servidor");
        UpdateStatus("Conectado ✓");
        UpdatePlayerCount();
    }

    private void OnDisconnectedFromServer()
    {
        Debug.Log("[LobbyUI] Desconectado del servidor");
        UpdateStatus("Desconectado");
        ShowMainMenu();
    }

    private void OnNetworkError(string error)
    {
        Debug.LogError($"[LobbyUI] Error de red: {error}");
        UpdateStatus($"Error: {error}");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}