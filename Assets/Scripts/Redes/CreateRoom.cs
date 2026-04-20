using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoom : MonoBehaviour
{
    [SerializeField]
    private InputField roomNameInput;

    /// <summary>
    /// Botón para crear sala - llamar desde UI Button onClick event
    /// </summary>
    public void OnCreateRoomButtonClicked()
    {
        string roomName = roomNameInput.text;
        
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
    /// Limpiar el input después de crear sala
    /// </summary>
    public void ClearRoomNameInput()
    {
        if (roomNameInput != null)
        {
            roomNameInput.text = "";
        }
    }
}

