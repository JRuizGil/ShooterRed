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
    private bool _playerInstantiated = false;
    
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
        if (this == null || gameObject == null) return;

        // Detener y destruir cualquier runner previo para limpiar el estado completamente
        var existingRunner = GetComponent<NetworkRunner>();
        if (existingRunner != null)
        {
            await existingRunner.Shutdown();
            
            // CRITICO: Después de un await, el objeto puede haber sido destruido
            if (this == null || gameObject == null) return;
            
            Destroy(existingRunner);
        }
            
        _playerInstantiated = false;

        // Crear un nuevo Runner
        runner = gameObject.GetComponent<NetworkRunner>();
        if (runner == null) runner = gameObject.AddComponent<NetworkRunner>();
        
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        // IMPORTANTE: Verifica tu Build Settings. 
        // Si PlayerScene es la segunda escena, usa FromIndex(1).
        var scene = SceneRef.FromIndex(2);
        
        var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null) sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        Debug.Log($"[NetworkRunnerHandler] Iniciando modo {mode} en sesión {sessionName}...");

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = 10,
            Scene = scene,
            SceneManager = sceneManager
        });
        
        if (!result.Ok)
        {
            Debug.LogError($"[NetworkRunnerHandler] StartGame failed: {result.ShutdownReason}");
            if (LobbyManager.Instance != null)
            { // Assuming LobbyManager has a public method to trigger its OnNetworkError event
                LobbyManager.Instance.TriggerNetworkError($"No se pudo iniciar la sala: {result.ShutdownReason}");
            }

            runner.RemoveCallbacks(this);
            Destroy(runner);
            Destroy(sceneManager);
            runner = null;
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Network] Player joined: {player}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayerJoinedNetwork(runner, player);
        }

        if (player == runner.LocalPlayer) {
            TrySpawnPlayer();
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
        
        // Enviar el input al NetworkRunner
        input.Set(playerNetworkInput);
    }
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Network] Shutdown: {shutdownReason}");
        if (LobbyManager.Instance != null)
        {
            _playerInstantiated = false;
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

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[Network] Scene load done");
        TrySpawnPlayer();
    }

    private void TrySpawnPlayer() {
        InstancePlayer();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[Network] Scene load start");
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[Network] Disconnected from server. Reason: {reason}");
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnDisconnectedFromServer(runner);
        }
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void InstancePlayer()
    {
        if (runner == null || !runner.IsRunning) return;
        if (runner.LocalPlayer == PlayerRef.None) return;

        // Verificación robusta de escena
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName != "PlayerScene")
        {
            Debug.LogWarning($"[NetworkRunnerHandler] Esperando a estar en PlayerScene (Actual: {activeSceneName})");
            return;
        }

        if (_playerInstantiated) return;

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
            _playerInstantiated = true;
        }
    }
}
