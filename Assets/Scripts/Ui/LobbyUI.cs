using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField sessionCodeInput;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI lobbyStatusText;

    private void Start()
    {
        // Configurar listeners
        createLobbyButton.onClick.AddListener(OnCreateLobbyClick);
        joinLobbyButton.onClick.AddListener(OnJoinLobbyClick);
        startGameButton.onClick.AddListener(OnStartGameClick);

        startGameButton.interactable = false;

        // Asegurar que el LobbyManager exista
        if (LobbyManager.Instance == null)
        {
            new GameObject("LobbyManager").AddComponent<LobbyManager>();
        }
    }

    private void OnDestroy()
    {
        if (createLobbyButton != null) createLobbyButton.onClick.RemoveListener(OnCreateLobbyClick);
        if (joinLobbyButton != null) joinLobbyButton.onClick.RemoveListener(OnJoinLobbyClick);
        if (startGameButton != null) startGameButton.onClick.RemoveListener(OnStartGameClick);
    }

    private async void OnCreateLobbyClick()
    {
        string sessionCode = sessionCodeInput.text.Trim();
        if (string.IsNullOrEmpty(sessionCode))
        {
            ShowError("¡Introduce un código/nombre para la sala!");
            return;
        }

        LockUI(true);
        lobbyStatusText.text = "Creando sala...";
        lobbyStatusText.color = Color.yellow;

        // Intentar crear (Photon validará si ya existe)
        bool success = await LobbyManager.Instance.CreateLobbyAsync(sessionCode);

        if (success)
        {
            lobbyStatusText.text = $"Hosteando: {sessionCode}";
            lobbyStatusText.color = Color.green;
            startGameButton.interactable = true;
        }
        else
        {
            ShowError("Fallo. ¿El nombre de la sala ya existe?");
            LockUI(false);
        }
    }

    private async void OnJoinLobbyClick()
    {
        string sessionCode = sessionCodeInput.text.Trim();
        if (string.IsNullOrEmpty(sessionCode))
        {
            ShowError("¡Introduce el código de la sala a la que te unes!");
            return;
        }

        LockUI(true);
        lobbyStatusText.text = "Buscando sala...";
        lobbyStatusText.color = Color.yellow;

        // Intentar unir (Photon validará si NO existe)
        bool success = await LobbyManager.Instance.JoinLobbyAsync(sessionCode);

        if (success)
        {
            lobbyStatusText.text = $"Unido a: {sessionCode}";
            lobbyStatusText.color = Color.cyan;
            // Solo el host puede iniciar, el cliente espera
            startGameButton.interactable = false;
        }
        else
        {
            ShowError("Sala no encontrada o está llena.");
            LockUI(false);
        }
    }

    private void OnStartGameClick()
    {
        if (LobbyManager.Instance != null && LobbyManager.Instance.IsHost)
        {
            // Reemplaza "PlayerScene" por el nombre exacto de tu escena de juego
            LobbyManager.Instance.StartGame("PlayerScene");
        }
    }

    private void LockUI(bool isLocked)
    {
        createLobbyButton.interactable = !isLocked;
        joinLobbyButton.interactable = !isLocked;
        playerNameInput.interactable = !isLocked;
        sessionCodeInput.interactable = !isLocked;
    }

    private void ShowError(string message)
    {
        Debug.LogWarning($"[LobbyUI] Error: {message}");
        lobbyStatusText.text = $"ERROR: {message}";
        lobbyStatusText.color = Color.red;
    }
}