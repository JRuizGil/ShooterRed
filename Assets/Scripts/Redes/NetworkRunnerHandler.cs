using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;

    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private SpawnPointManager spawnPointManager;

    // Registro de jugadores ya spawneados — evita duplicados
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

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

    // =========================================================
    // CONEXIÓN AL LOBBY DE FUSION (descubrimiento de sesiones)
    // =========================================================
    public async void ConnectToLobby()
    {
        if (runner != null)
        {
            await runner.Shutdown();
            runner = null;
        }

        runner = GetOrAddRunner();
        runner.ProvideInput = false;
        runner.AddCallbacks(this);

        var result = await runner.JoinSessionLobby(SessionLobby.ClientServer);

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkRunnerHandler] Error lobby: {result.ShutdownReason}");
            LobbyManager.Instance?.NotifyNetworkError($"Error: {result.ShutdownReason}");
        }
        else
        {
            Debug.Log("[NetworkRunnerHandler] Conectado al lobby de Fusion");
        }
    }

    // =========================================================
    // CREAR SALA — host sin cargar escena de juego todavía
    // =========================================================
    public async void CreateLobbySession(string sessionName)
    {
        await ShutdownRunnerIfRunning();

        runner = GetOrAddRunner();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        int currentScene = SceneManager.GetActiveScene().buildIndex;
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode     = GameMode.Host,
            SessionName  = sessionName,
            Scene        = SceneRef.FromIndex(currentScene),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkRunnerHandler] Error crear sala: {result.ShutdownReason}");
            LobbyManager.Instance?.NotifyNetworkError($"Error: {result.ShutdownReason}");
        }
        else
        {
            Debug.Log($"[NetworkRunnerHandler] Sala creada: {sessionName}");
        }
    }

    // =========================================================
    // UNIRSE A SALA existente
    // =========================================================
    public async void JoinLobbySession(string sessionName)
    {
        await ShutdownRunnerIfRunning();

        runner = GetOrAddRunner();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        int currentScene = SceneManager.GetActiveScene().buildIndex;
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode     = GameMode.Client,
            SessionName  = sessionName,
            Scene        = SceneRef.FromIndex(currentScene),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkRunnerHandler] Error unirse: {result.ShutdownReason}");
            LobbyManager.Instance?.NotifyNetworkError($"Error: {result.ShutdownReason}");
        }
        else
        {
            Debug.Log($"[NetworkRunnerHandler] Unido a sala: {sessionName}");
        }
    }

    // =========================================================
    // CARGAR ESCENA DE JUEGO — solo el host
    // =========================================================
    public void LoadGameScene()
    {
        if (runner == null || !runner.IsRunning)
        {
            Debug.LogError("[NetworkRunnerHandler] No hay runner activo");
            return;
        }
        runner.LoadScene(SceneRef.FromIndex(2));
        Debug.Log("[NetworkRunnerHandler] Cargando escena de juego...");
    }

    // =========================================================
    // SPAWN
    // =========================================================

    // OnPlayerJoined: spawnear si la escena de juego ya está activa
    // (jugadores que entran DESPUÉS de que la partida empezó)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkRunnerHandler] OnPlayerJoined: {player} | " +
                  $"IsServer:{runner.IsServer} | IsGameScene:{IsGameScene()} | " +
                  $"YaSpawneado:{spawnedPlayers.ContainsKey(player)}");

        if (runner.IsServer && IsGameScene() && !spawnedPlayers.ContainsKey(player))
        {
            Debug.Log($"[NetworkRunnerHandler] Spawneando a {player} desde OnPlayerJoined");
            SpawnPlayer(runner, player);
        }

        LobbyManager.Instance?.OnPlayerJoinedNetwork(runner, player);
    }

    // OnSceneLoadDone: spawnear todos los jugadores activos al cargar la escena
    // (carga inicial cuando el host pulsa "Iniciar Partida")
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log($"[NetworkRunnerHandler] OnSceneLoadDone: {SceneManager.GetActiveScene().name} " +
                  $"buildIndex:{SceneManager.GetActiveScene().buildIndex} IsServer:{runner.IsServer}");

        if (!IsGameScene()) return;
        if (!runner.IsServer) return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!spawnedPlayers.ContainsKey(player))
            {
                Debug.Log($"[NetworkRunnerHandler] OnSceneLoadDone → Spawneando {player}");
                SpawnPlayer(runner, player);
            }
            else
            {
                Debug.Log($"[NetworkRunnerHandler] OnSceneLoadDone → {player} ya spawneado");
            }
        }
    }

    private bool IsGameScene()
    {
        return SceneManager.GetActiveScene().buildIndex == 2;
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedPlayers.ContainsKey(player))
        {
            Debug.LogWarning($"[NetworkRunnerHandler] {player} ya spawneado, ignorando");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkRunnerHandler] playerPrefab no asignado en el Inspector");
            return;
        }

        if (spawnPointManager == null)
            spawnPointManager = FindFirstObjectByType<SpawnPointManager>();

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (spawnPointManager != null && spawnPointManager.GetSpawnPointCount() > 0)
        {
            (spawnPos, spawnRot) = spawnPointManager.GetNextSpawnPoint();
        }
        else
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle * 5f;
            spawnPos = new Vector3(r.x, 1f, r.y);
            spawnRot = Quaternion.identity;
            Debug.LogWarning("[NetworkRunnerHandler] SpawnPointManager no encontrado");
        }

        NetworkObject obj = runner.Spawn(playerPrefab, spawnPos, spawnRot, player);

        if (obj == null)
        {
            Debug.LogError($"[NetworkRunnerHandler] Falló el spawn de {player}");
        }
        else
        {
            spawnedPlayers[player] = obj;
            Debug.Log($"[NetworkRunnerHandler] {player} spawneado en {spawnPos}");
        }
    }

    // =========================================================
    // CALLBACKS
    // =========================================================

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkRunnerHandler] Player left: {player}");

        if (spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            if (obj != null) runner.Despawn(obj);
            spawnedPlayers.Remove(player);
        }

        LobbyManager.Instance?.OnPlayerLeftNetwork(runner, player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new PlayerNetworkInput();

        data.MoveDirection = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );

        // ✅ Enviar la rotación Y actual del jugador local con el input
        // El host la usará para calcular la dirección de movimiento correcta
        // en vez de usar su rotación interpolada (que tiene retraso)
        var localPlayer = runner.GetPlayerObject(runner.LocalPlayer);
        if (localPlayer != null)
            data.YawAngle = localPlayer.transform.eulerAngles.y;

        if (Input.GetKey(KeyCode.Space))    data.Buttons.Set(PlayerButtons.Jump, true);
        if (Input.GetMouseButton(0))        data.Buttons.Set(PlayerButtons.Fire, true);
        if (Input.GetKey(KeyCode.E))        data.Buttons.Set(PlayerButtons.NextWeapon, true);
        if (Input.GetKey(KeyCode.Q))        data.Buttons.Set(PlayerButtons.PrevWeapon, true);

        input.Set(data);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[NetworkRunnerHandler] Shutdown: {shutdownReason}");
        spawnedPlayers.Clear();
        LobbyManager.Instance?.OnNetworkShutdown(runner, shutdownReason);
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[NetworkRunnerHandler] Conectado al servidor");
        LobbyManager.Instance?.NotifyConnectedToServer(runner);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[NetworkRunnerHandler] Desconectado: {reason}");
        LobbyManager.Instance?.NotifyDisconnectedFromServer(runner);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[NetworkRunnerHandler] Sesiones: {sessionList.Count}");
        LobbyManager.Instance?.OnSessionListUpdatedNetwork(sessionList);
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[NetworkRunnerHandler] Cargando escena...");
        // Limpiar registro al cambiar de escena para permitir re-spawn limpio
        spawnedPlayers.Clear();
    }

    // Mantener por compatibilidad
    public void InstancePlayer()
    {
        if (runner != null && runner.IsServer)
            SpawnPlayer(runner, runner.LocalPlayer);
    }

    public async void StartGame(GameMode mode, string sessionName)
    {
        await ShutdownRunnerIfRunning();
        runner = GetOrAddRunner();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode     = mode,
            SessionName  = sessionName,
            Scene        = SceneRef.FromIndex(2),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkRunnerHandler] Error StartGame: {result.ShutdownReason}");
            LobbyManager.Instance?.NotifyNetworkError($"Error: {result.ShutdownReason}");
        }
    }

    private async System.Threading.Tasks.Task ShutdownRunnerIfRunning()
    {
        if (runner != null && runner.IsRunning)
        {
            await runner.Shutdown();
            runner = null;
        }
    }

    private NetworkRunner GetOrAddRunner()
    {
        NetworkRunner r = gameObject.GetComponent<NetworkRunner>();
        if (r == null) r = gameObject.AddComponent<NetworkRunner>();
        return r;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[NetworkRunnerHandler] Conexión fallida: {reason}");
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("[NetworkRunnerHandler] Host migration");
    }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}