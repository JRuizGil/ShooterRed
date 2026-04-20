using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

//public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
//{
//    private NetworkRunner runner;

//    public async void StartGame(GameMode mode, string sessionName)
//    {
//        if (runner != null)
//            return;

//        runner = gameObject.AddComponent<NetworkRunner>();
//        runner.ProvideInput = true;

//        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

//        var result = await runner.StartGame(new StartGameArgs()
//        {
//            GameMode = mode,
//            SessionName = sessionName,
//            Scene = scene,
//            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
//        });

//        if (!result.Ok)
//        {
//            Debug.LogError("Error: " + result.ShutdownReason);
//        }
//    }

//    // Métodos obligatorios (déjalos vacíos de momento)
//    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
//    {
//        Debug.Log("Jugador unido: " + player);
//    }

//    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
//    public void OnInput(NetworkRunner runner, NetworkInput input) { }
//    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
//    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
//    public void OnConnectedToServer(NetworkRunner runner) { }
//    public void OnDisconnectedFromServer(NetworkRunner runner) { }
//    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
//    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
//    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
//    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
//    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
//    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
//}