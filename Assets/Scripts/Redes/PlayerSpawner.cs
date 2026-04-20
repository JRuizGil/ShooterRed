using Fusion;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private NetworkObject playerPrefab;

    [SerializeField]
    private SpawnPointManager spawnPointManager;

    private NetworkRunner currentRunner;

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player prefab not assigned!");
        }

        if (spawnPointManager == null)
        {
            spawnPointManager = GetComponent<SpawnPointManager>();
            if (spawnPointManager == null)
            {
                spawnPointManager = FindFirstObjectByType<SpawnPointManager>();
            }
        }

        if (spawnPointManager == null)
        {
            Debug.LogError("[PlayerSpawner] SpawnPointManager not found!");
        }

        // Suscribirse a eventos de lobby para actualizar referencias
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayerJoinedSession += HandlePlayerJoined;
            LobbyManager.Instance.OnPlayerLeftSession += HandlePlayerLeft;
        }
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayerJoinedSession -= HandlePlayerJoined;
            LobbyManager.Instance.OnPlayerLeftSession -= HandlePlayerLeft;
        }
    }

    /// <summary>
    /// Llamado cuando un jugador entra a la sesión
    /// Spawnea el prefab de jugador en el siguiente punto de spawn
    /// </summary>
    private void HandlePlayerJoined(PlayerRef playerRef)
    {
        if (!LobbyManager.Instance.IsHost())
        {
            // Solo el host hace el spawn
            return;
        }

        NetworkRunner runner = LobbyManager.Instance.GetCurrentRunner();
        if (runner == null)
        {
            Debug.LogError("[PlayerSpawner] No runner available when trying to spawn player");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player prefab not assigned!");
            return;
        }

        if (spawnPointManager == null)
        {
            Debug.LogError("[PlayerSpawner] SpawnPointManager not available!");
            return;
        }

        // Obtener posición y rotación del siguiente spawn point
        var (spawnPos, spawnRot) = spawnPointManager.GetNextSpawnPoint();

        Debug.Log($"[PlayerSpawner] Spawning player {playerRef} at position {spawnPos}");

        // Spawn del jugador con inicialización previa
        NetworkObject spawnedPlayer = runner.Spawn(
            playerPrefab,
            spawnPos,
            spawnRot,
            playerRef,
            onBeforeSpawned: (NetworkRunner r, NetworkObject obj) =>
            {
                InitializePlayerBeforeSpawn(obj, playerRef);
            }
        );

        if (spawnedPlayer == null)
        {
            Debug.LogError("[PlayerSpawner] Failed to spawn player");
        }
    }

    /// <summary>
    /// Inicializar propiedades de red del jugador ANTES de que se sincronice
    /// </summary>
    private void InitializePlayerBeforeSpawn(NetworkObject playerObject, PlayerRef playerRef)
    {
        PlayerState playerState = playerObject.GetComponent<PlayerState>();
        if (playerState == null)
        {
            Debug.LogWarning("[PlayerSpawner] PlayerState component not found on prefab");
            return;
        }

        // Establecer el propietario del jugador
        playerState.OwnerPlayer = playerRef;
        
        // Inicializar health y otras propiedades según necesites
        playerState.Health = 100; // Valor por defecto

        Debug.Log($"[PlayerSpawner] Initialized player {playerRef} with health: {playerState.Health}");
    }

    /// <summary>
    /// Llamado cuando un jugador sale de la sesión
    /// Aquí podrías fazer despawn o limpiar referencias
    /// </summary>
    private void HandlePlayerLeft(PlayerRef playerRef)
    {
        Debug.Log($"[PlayerSpawner] Player left: {playerRef}");
        // El despawn se puede hacer aquí si es necesario
        // Por ahora, Fusion maneja automáticamente the cleanup
    }

    /// <summary>
    /// Reiniciar spawn points para nueva partida
    /// </summary>
    public void ResetSpawnPoints()
    {
        if (spawnPointManager != null)
        {
            spawnPointManager.ResetSpawnIndex();
        }
    }
}
