using Fusion;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.Collections.Unicode;
public class CreateRoom : NetworkBehaviour
{
    [SerializeField]
    public InputField sceneName;
    public async Task<StartGameResult> CreateRoomAsync(NetworkRunner runner, string roomName)
    {
        StartGameArgs args = new StartGameArgs { GameMode = GameMode.Shared, SessionName = roomName};
        StartGameResult result = await runner.StartGame(args);
        return result;
    }
    public void CreateRoomButton(InputField sceneName)
    {
       CreateRoomAsync(NetworkManager.Instance.Runner, sceneName.text);
    }
}
