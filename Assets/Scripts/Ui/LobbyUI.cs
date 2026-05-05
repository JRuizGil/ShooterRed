using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI del Lobby - Menú para crear/buscar salas y entrar a partidas
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Player Name Input")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button startGameButton;

    [Header("Room Creation")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField maxPlayersInput;
    [SerializeField] private Button createRoomButton;

    [Header("Room List")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomListItemPrefab;
    [SerializeField] private Button refreshRoomsButton;

    [Header("UI Panels")]
    [SerializeField] private CanvasGroup lobbyPanel;
    [SerializeField] private CanvasGroup createRoomPanel;
    [SerializeField] private CanvasGroup joinRoomPanel;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;

    private LobbyManager lobbyManager;
    private string currentPlayerName = "";

    private void Start()
    {
        lobbyManager = LobbyManager.Instance;

        // Configurar listeners de botones
        if (playerNameInput != null)
            playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);

        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);

        if (refreshRoomsButton != null)
            refreshRoomsButton.onClick.AddListener(OnRefreshRoomsClicked);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);

        // Cargar nombre guardado si existe
        string savedName = PlayerPrefs.GetString("PlayerName", "Player_" + Random.Range(1000, 9999));
        if (playerNameInput != null)
            playerNameInput.text = savedName;

        UpdateUI();
        Debug.Log("[LobbyUI] Initialized");
    }

    private void OnPlayerNameChanged(string newName)
    {
        currentPlayerName = newName;
        PlayerPrefs.SetString("PlayerName", newName);
        PlayerPrefs.Save();
        UpdateStartButtonState();
    }

    private void OnCreateRoomClicked()
    {
        if (string.IsNullOrEmpty(currentPlayerName))
        {
            ShowStatus("Por favor introduce tu nombre", Color.red);
            return;
        }

        string roomName = roomNameInput != null ? roomNameInput.text : "Room_" + Random.Range(1000, 9999);
        int maxPlayers = 2;

        if (maxPlayersInput != null && int.TryParse(maxPlayersInput.text, out int parsedMax))
            maxPlayers = parsedMax;

        if (lobbyManager != null)
        {
            lobbyManager.CreateRoom(roomName);
            ShowStatus($"Creando sala: {roomName}", Color.yellow);
        }
    }

    private void OnRefreshRoomsClicked()
    {
        if (lobbyManager != null)
        {
            ShowStatus("Actualizando salas...", Color.yellow);
            DisplayRoomList(lobbyManager.GetAvailableSessions());
        }
    }

    private void OnStartGameClicked()
    {
        if (string.IsNullOrEmpty(currentPlayerName))
        {
            ShowStatus("Por favor introduce tu nombre", Color.red);
            return;
        }

        if (lobbyManager != null && lobbyManager.IsHost())
        {
            ShowStatus("Ya eres host. Espera a otros jugadores.", Color.green);
        }
        else
        {
            ShowStatus("Crea o únete a una sala para iniciar el juego.", Color.yellow);
        }
    }

    /// <summary>
    /// Unirse a una sala
    /// </summary>
    public void JoinRoom(string roomName)
    {
        if (string.IsNullOrEmpty(currentPlayerName))
        {
            ShowStatus("Por favor introduce tu nombre", Color.red);
            return;
        }

        if (lobbyManager != null)
        {
            lobbyManager.JoinRoom(roomName);
            ShowStatus($"Uniéndose a: {roomName}", Color.yellow);
        }
    }

    /// <summary>
    /// Mostrar lista de salas disponibles
    /// </summary>
    public void DisplayRoomList(List<SessionInfo> rooms)
    {
        if (roomListContent == null || roomListItemPrefab == null)
            return;

        // Limpiar lista anterior
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        if (rooms.Count == 0)
        {
            ShowStatus("No hay salas disponibles. ¡Crea una!", Color.yellow);
            return;
        }

        // Crear items para cada sala
        foreach (SessionInfo room in rooms)
        {
            GameObject item = Instantiate(roomListItemPrefab, roomListContent);
            
            TextMeshProUGUI[] textComponents = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length >= 2)
            {
                textComponents[0].text = room.Name;
                textComponents[1].text = $"{room.PlayerCount}/{room.MaxPlayers}";
            }

            Button joinButton = item.GetComponentInChildren<Button>();
            if (joinButton != null)
            {
                string roomNameCopy = room.Name;
                joinButton.onClick.AddListener(() => JoinRoom(roomNameCopy));
            }
        }

        ShowStatus($"Se encontraron {rooms.Count} salas", Color.green);
    }

    private void UpdateStartButtonState()
    {
        if (startGameButton != null)
        {
            startGameButton.interactable = !string.IsNullOrEmpty(currentPlayerName) && 
                                          currentPlayerName.Length >= 2;
        }
    }

    private void UpdateUI()
    {
        UpdateStartButtonState();
    }

    private void ShowStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
            Debug.Log($"[LobbyUI] Status: {message}");
        }
    }

    // Setter para cambiar visibilidad de paneles
    public void ShowCreateRoomPanel(bool show)
    {
        if (createRoomPanel != null)
            createRoomPanel.alpha = show ? 1 : 0;
    }

    public void ShowJoinRoomPanel(bool show)
    {
        if (joinRoomPanel != null)
            joinRoomPanel.alpha = show ? 1 : 0;
    }
}
