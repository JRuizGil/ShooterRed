using Fusion;
using UnityEngine;

/// <summary>
/// Gestor de respawn
/// Coordina el respawn de jugadores, protección de spawn y reposicionamiento
/// </summary>
public class RespawnManager : NetworkBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private bool enableSpawnProtection = true;

    [Header("References")]
    [SerializeField] private SpawnPointManager spawnPointManager;

    private PlayerHealth playerHealth;
    private SpawnProtection spawnProtection;
    private PlayerMovements playerMovements;
    private CharacterController characterController;
    private float respawnCountdown = 0f;
    private bool waitingForRespawn = false;

    public override void Spawned()
    {
        Debug.Log("[RespawnManager] Respawn manager initialized");

        playerHealth = GetComponent<PlayerHealth>();
        spawnProtection = GetComponent<SpawnProtection>();
        playerMovements = GetComponent<PlayerMovements>();
        characterController = GetComponent<CharacterController>();

        if (spawnPointManager == null)
            spawnPointManager = FindFirstObjectByType<SpawnPointManager>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || playerHealth == null)
            return;

        // Verificar si el jugador está muerto y manejar respawn
        if (!playerHealth.GetIsAlive() && !waitingForRespawn)
        {
            StartRespawnProcess();
        }

        // Contar hacia el respawn
        if (waitingForRespawn)
        {
            respawnCountdown -= Runner.DeltaTime;

            if (respawnCountdown <= 0)
            {
                ExecuteRespawn();
            }
        }
    }

    /// <summary>
    /// Iniciar el proceso de respawn
    /// </summary>
    private void StartRespawnProcess()
    {
        waitingForRespawn = true;
        respawnCountdown = respawnDelay;

        Debug.Log($"[RespawnManager] Starting respawn process. Waiting {respawnDelay}s");

        // Desactivar controles
        if (playerMovements != null)
            playerMovements.enabled = false;

        // Notificar UI
        if (GameHUD.Instance != null)
        {
            // Mostrar contador de respawn
        }
    }

    /// <summary>
    /// Ejecutar el respawn
    /// </summary>
    private void ExecuteRespawn()
    {
        waitingForRespawn = false;

        // Obtener nuevo punto de spawn
        if (spawnPointManager == null)
        {
            Debug.LogError("[RespawnManager] SpawnPointManager no asignado!");
            return;
        }

        var (spawnPos, spawnRot) = spawnPointManager.GetNextSpawnPoint();

        // Teletransportar al jugador
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = spawnPos;
            transform.rotation = spawnRot;
            characterController.enabled = true;
        }
        else
        {
            Debug.LogWarning("[RespawnManager] CharacterController no encontrado, usando transform directo");
            transform.position = spawnPos;
            transform.rotation = spawnRot;
        }

        // Resetear velocidad
        if (playerMovements != null)
        {
            playerMovements.enabled = true;
        }

        // Activar protección de spawn
        if (enableSpawnProtection && spawnProtection != null)
        {
            spawnProtection.ActivateSpawnProtection();
        }

        // Resucitar jugador
        playerHealth.PublicRespawn();

        Debug.Log($"[RespawnManager] Player respawned at {spawnPos}");
    }

    /// <summary>
    /// Getter para saber si está esperando respawn
    /// </summary>
    public bool IsWaitingForRespawn()
    {
        return waitingForRespawn;
    }

    /// <summary>
    /// Getter para el tiempo de espera restante
    /// </summary>
    public float GetRespawnCountdown()
    {
        return Mathf.Max(0, respawnCountdown);
    }
}
