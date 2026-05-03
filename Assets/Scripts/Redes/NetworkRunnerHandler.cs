using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;
    
    [SerializeField] private NetworkObject playerPrefab;
    
    public static NetworkRunnerHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void StartGame(GameMode mode, string sessionName)
    {
        if (runner != null)
            return;

        runner = gameObject.GetComponent<NetworkRunner>();
        if (runner == null) runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        // Load PlayerScene (build index 2)
        var scene = SceneRef.FromIndex(2);

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        
        if (!result.Ok)
        {
            Debug.LogError("Error: " + result.ShutdownReason);
        }
        
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Network] Player joined: {player}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayerJoinedNetwork(runner, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Network] Player left: {player}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayerLeftNetwork(runner, player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var playerNetworkInput = new PlayerNetworkInput();
        
        // Capturar dirección de movimiento
        playerNetworkInput.MoveDirection = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        
        // Capturar botones
        if (Input.GetKey(KeyCode.Space))
            playerNetworkInput.Buttons.Set(PlayerButtons.Jump, true);
        if (Input.GetMouseButton(0))
            playerNetworkInput.Buttons.Set(PlayerButtons.Fire, true);
        
        // Debug para verificar que se está capturando input
        if (playerNetworkInput.MoveDirection.sqrMagnitude > 0)
        {
            Debug.Log($"[NetworkRunnerHandler] Input capturado: {playerNetworkInput.MoveDirection}");
        }
        
        // Enviar el input al NetworkRunner
        input.Set(playerNetworkInput);
    }
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Network] Shutdown: {shutdownReason}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnNetworkShutdown(runner, shutdownReason);
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[Network] Connected to server");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnConnectedToServer(runner);
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        Debug.Log("[Network] Disconnected from server");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnDisconnectedFromServer(runner);
        }
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[Network] Connect failed: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[Network] Session list updated: {sessionList.Count} sessions");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnSessionListUpdatedNetwork(sessionList);
        }
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("[Network] Host migration detected");
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[Network] Scene load done");
        InstancePlayer();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[Network] Scene load start");
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    public void InstancePlayer()
    {
        // Verificar que estamos en la escena PlayerScene
        if (SceneManager.GetActiveScene().name != "PlayerScene")
        {
            Debug.LogWarning("[NetworkRunnerHandler] InstancePlayer() called but scene is not PlayerScene");
            return;
        }

        if (runner == null)
        {
            Debug.LogError("[NetworkRunnerHandler] NetworkRunner no disponible");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkRunnerHandler] Player prefab no asignado en el inspector");
            return;
        }

        // Generar posición aleatoria dentro de 100 unidades a la redonda
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 100f;
        Vector3 spawnPosition = new Vector3(randomCircle.x, 1f, randomCircle.y);

        Debug.Log($"[NetworkRunnerHandler] InstancePlayer() - Spawnando jugador en posición: {spawnPosition}");

        // Hacer spawn networkizado del prefab del jugador
        NetworkObject spawnedPlayer = runner.Spawn(
            playerPrefab,
            spawnPosition,
            Quaternion.identity,
            runner.LocalPlayer
        );

        if (spawnedPlayer == null)
        {
            Debug.LogError("[NetworkRunnerHandler] Falló el spawn del jugador");
        }
        else
        {
            Debug.Log($"[NetworkRunnerHandler] Jugador spawneado exitosamente: {spawnedPlayer.InputAuthority}");
        }
    }
}
