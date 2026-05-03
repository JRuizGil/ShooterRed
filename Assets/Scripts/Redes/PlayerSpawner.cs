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

        // If no SpawnPointManager exists, create one with default spawn points
        if (spawnPointManager == null)
        {
            Debug.LogWarning("[PlayerSpawner] SpawnPointManager not found, creating one with default spawn points");
            CreateDefaultSpawnPoints();
        }

        // Subscribe to lobby events for player spawning
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayerJoinedSession += HandlePlayerJoined;
            LobbyManager.Instance.OnPlayerLeftSession += HandlePlayerLeft;
        }
    }

    /// <summary>
    /// Create default spawn points if none exist in the scene
    /// </summary>
    private void CreateDefaultSpawnPoints()
    {
        // Create SpawnPointsManager GameObject
        GameObject spawnManagerObj = new GameObject("SpawnPointsManager");
        spawnPointManager = spawnManagerObj.AddComponent<SpawnPointManager>();

        // Create 4 spawn points in different locations
        Vector3[] spawnPositions = new Vector3[]
        {
            new Vector3(-5, 1, -5),   // Front-Left
            new Vector3(5, 1, -5),    // Front-Right
            new Vector3(-5, 1, 5),    // Back-Left
            new Vector3(5, 1, 5)      // Back-Right
        };

        var spawnPoints = new System.Collections.Generic.List<Transform>();

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            GameObject spawnPointObj = new GameObject($"SpawnPoint_{i + 1}");
            spawnPointObj.transform.position = spawnPositions[i];
            spawnPointObj.transform.rotation = Quaternion.identity;
            spawnPoints.Add(spawnPointObj.transform);

            Debug.Log($"[PlayerSpawner] Created spawn point {i + 1} at {spawnPositions[i]}");
        }

        // Assign spawn points to SpawnPointManager via reflection
        var field = typeof(SpawnPointManager).GetField("spawnPoints", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(spawnPointManager, spawnPoints);
            Debug.Log("[PlayerSpawner] Assigned spawn points to SpawnPointManager");
        }
        else
        {
            Debug.LogError("[PlayerSpawner] Could not find spawnPoints field in SpawnPointManager");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from lobby events
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
        NetworkRunner runner = LobbyManager.Instance.GetCurrentRunner();
        if (runner == null)
        {
            Debug.LogError("[PlayerSpawner] No runner available when trying to spawn player");
            return;
        }

        // Verificar si este es el jugador local
        bool isLocalPlayer = runner.LocalPlayer == playerRef;
        Debug.Log($"[PlayerSpawner] Player joined: {playerRef}, IsLocal: {isLocalPlayer}, LocalPlayer: {runner.LocalPlayer}");

        // EN SHARED MODE: Cada cliente spawnea SOLO a su propio jugador.
        // Fusion se encarga de replicar ese objeto en los demás clientes automáticamente.
        if (!isLocalPlayer)
        {
            Debug.Log($"[PlayerSpawner] Ignorando spawn de jugador remoto {playerRef}. Fusion lo replicará.");
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
        else
        {
            Debug.Log($"[PlayerSpawner] Successfully spawned player {playerRef}");
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
