using Fusion;
using UnityEngine;
using TMPro;

public class CreateRoom : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField roomNameInput;

    // Crea una sala con el nombre del InputField (llamado desde onClick del botón)
    public void OnCreateRoomButtonClicked()
    {
        if (roomNameInput == null)
        {
            Debug.LogError("[CreateRoom] roomNameInput no está asignado en el Inspector.");
            return;
        }

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

    // Borra el texto del InputField
    public void ClearRoomNameInput()
    {
        if (roomNameInput != null)
        {
            roomNameInput.text = "";
        }
    }
}
