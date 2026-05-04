using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using System.Linq; // Necesario para contar jugadores fácilmente

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public string SessionCode { get; private set; }
    public bool GameStarted { get; set; }

    // Diccionario restaurado para que GetPlayerNames() funcione[cite: 1]
    private Dictionary<PlayerRef, string> connectedPlayers = new();

    private NetworkRunner _runner;

    public bool IsHost => _runner != null && _runner.IsServer;

    // Corregido: Usa SessionInfo para obtener el conteo real de la sala
    public int PlayerCount => (_runner != null && _runner.SessionInfo != null)
        ? _runner.SessionInfo.PlayerCount
        : connectedPlayers.Count;

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

    // --- MÉTODOS DE CONEXIÓN ---

    public async Task<bool> CreateLobbyAsync(string sessionName)
    {
        return await StartSessionAsync(sessionName, GameMode.Host);
    }

    public async Task<bool> JoinLobbyAsync(string sessionName)
    {
        return await StartSessionAsync(sessionName, GameMode.Client);
    }

    private async Task<bool> StartSessionAsync(string sessionName, GameMode mode)
    {
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }

        SessionCode = sessionName;

        var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null) sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            SceneManager = sceneManager
        });

        return result.Ok;
    }

    // --- MÉTODOS REQUERIDOS POR TUS OTROS SCRIPTS[cite: 1] ---

    public void RegisterPlayerData(PlayerRef player, PlayerData data)
    {
        // Este método lo llama PlayerData.cs
        Debug.Log($"[LobbyManager] Datos recibidos del jugador: {player}");
    }

    public void RegisterPlayer(PlayerRef player, string playerName)
    {
        if (!connectedPlayers.ContainsKey(player))
        {
            connectedPlayers.Add(player, playerName);
        }
    }

    public List<string> GetPlayerNames()
    {
        // Este método lo llama PlayerSpawner.cs
        return connectedPlayers.Values.ToList();
    }

    public void StartGame(string sceneName)
    {
        if (!IsHost) return;

        GameStarted = true;
        _runner.LoadScene(SceneRef.FromIndex(UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(sceneName)));
    }
}