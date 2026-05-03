using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateRoom : MonoBehaviour
{
    [Header("Create Room")]
    [SerializeField] private TMP_InputField createRoomNameInput;
    [SerializeField] private Button createRoomButton;

    [Header("Join Room")]
    [SerializeField] private TMP_InputField joinRoomNameInput;
    [SerializeField] private Button joinRoomButton;

    [Header("Panels")]
    [SerializeField] private GameObject createPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private Button showJoinPanelButton;
    [SerializeField] private Button showCreatePanelButton;

    private void Start()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
        }

        if (showJoinPanelButton != null)
        {
            showJoinPanelButton.onClick.AddListener(ShowJoinPanel);
        }

        if (showCreatePanelButton != null)
        {
            showCreatePanelButton.onClick.AddListener(ShowCreatePanel);
        }

        ShowCreatePanel();
    }

    /// <summary>
    /// Jugador 1 crea sala
    /// </summary>
    public void OnCreateRoomButtonClicked()
    {
        if (createRoomNameInput == null)
        {
            Debug.LogError("[CreateRoom] createRoomNameInput no está asignado en el Inspector.");
            return;
        }

        string roomName = createRoomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("[CreateRoom] Room name is empty!");
            return;
        }

        Debug.Log($"[CreateRoom] Creating room: {roomName}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.CreateRoom(roomName);
        }
        else
        {
            Debug.LogError("[CreateRoom] LobbyManager not found!");
        }
    }

    /// <summary>
    /// Jugador 2 se une a sala escribiendo el nombre
    /// </summary>
    public void OnJoinRoomButtonClicked()
    {
        if (joinRoomNameInput == null)
        {
            Debug.LogError("[CreateRoom] joinRoomNameInput no está asignado en el Inspector.");
            return;
        }

        string roomName = joinRoomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("[CreateRoom] Room name is empty!");
            return;
        }

        Debug.Log($"[CreateRoom] Joining room: {roomName}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.JoinRoom(roomName);
        }
        else
        {
            Debug.LogError("[CreateRoom] LobbyManager not found!");
        }
    }

    public void ShowCreatePanel()
    {
        if (createPanel != null) createPanel.SetActive(true);
        if (joinPanel != null) joinPanel.SetActive(false);
    }

    public void ShowJoinPanel()
    {
        if (createPanel != null) createPanel.SetActive(false);
        if (joinPanel != null) joinPanel.SetActive(true);
    }

    public void ClearInputs()
    {
        if (createRoomNameInput != null)
            createRoomNameInput.text = string.Empty;
        if (joinRoomNameInput != null)
            joinRoomNameInput.text = string.Empty;
    }
}
