using Fusion;
using UnityEngine;

public class RespawnManager : NetworkBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private bool enableSpawnProtection = true;

    [Header("References")]
    [SerializeField] private SpawnPointManager spawnPointManager;

    private PlayerHealth      playerHealth;
    private SpawnProtection   spawnProtection;
    private CharacterController characterController;

    // ✅ Networked para que todos los clientes sepan la posición de respawn
    [Networked] private Vector3 RespawnPosition { get; set; }
    [Networked] private float   RespawnYaw      { get; set; }
    [Networked] private NetworkBool IsRespawning { get; set; }

    private bool _wasAlive = true;

    public override void Spawned()
    {
        playerHealth        = GetComponent<PlayerHealth>();
        spawnProtection     = GetComponent<SpawnProtection>();
        characterController = GetComponent<CharacterController>();

        if (spawnPointManager == null)
            spawnPointManager = FindFirstObjectByType<SpawnPointManager>();

        _wasAlive = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || playerHealth == null) return;

        bool isAlive = playerHealth.GetIsAlive();

        // Detectar muerte
        if (_wasAlive && !isAlive && !IsRespawning)
        {
            PrepareRespawnPoint();
            IsRespawning = true;
        }

        _wasAlive = isAlive;

        // Contar respawn timer
        if (!isAlive && playerHealth.RespawnTimer <= 0f && IsRespawning)
        {
            ExecuteRespawn();
            IsRespawning = false;
        }
    }

    public override void Render()
    {
        // ✅ Aplicar posición de respawn en TODOS los clientes cuando cambia
        if (!playerHealth.GetIsAlive()) return;
        if (RespawnPosition == Vector3.zero) return;

        // Solo aplicar si acabamos de resucitar (posición cambió)
        if (Vector3.Distance(transform.position, RespawnPosition) > 0.5f)
        {
            if (characterController != null)
                characterController.enabled = false;

            transform.position = RespawnPosition;
            transform.rotation = Quaternion.Euler(0, RespawnYaw, 0);

            // Actualizar posición networked del movimiento
            PlayerMovements pm = GetComponent<PlayerMovements>();
            if (pm != null)
            {
                pm.NetworkedPosition  = RespawnPosition;
                pm.NetworkedRotationY = RespawnYaw;
            }

            if (characterController != null)
                characterController.enabled = Object.HasStateAuthority;
        }
    }

    private void PrepareRespawnPoint()
    {
        if (spawnPointManager == null)
        {
            Debug.LogError("[RespawnManager] SpawnPointManager no asignado!");
            return;
        }

        var (pos, rot) = spawnPointManager.GetNextSpawnPoint();
        RespawnPosition = pos;
        RespawnYaw      = rot.eulerAngles.y;
    }

    private void ExecuteRespawn()
    {
        if (characterController != null)
            characterController.enabled = false;

        transform.position = RespawnPosition;
        transform.rotation = Quaternion.Euler(0, RespawnYaw, 0);

        // ✅ Sincronizar posición en PlayerMovements
        PlayerMovements pm = GetComponent<PlayerMovements>();
        if (pm != null)
        {
            pm.NetworkedPosition  = RespawnPosition;
            pm.NetworkedRotationY = RespawnYaw;
            PlayerMovements.LocalYawAngle = RespawnYaw;
        }

        if (characterController != null)
            characterController.enabled = true;

        if (enableSpawnProtection && spawnProtection != null)
            spawnProtection.ActivateSpawnProtection();

        playerHealth.PublicRespawn();

        // Resetear velocidad
        RespawnPosition = Vector3.zero; // Reset para que Render() no siga aplicando

        Debug.Log($"[RespawnManager] Respawn ejecutado en {transform.position}");
    }

    public bool IsWaitingForRespawn() => IsRespawning;
    public float GetRespawnCountdown() => playerHealth != null ? Mathf.Max(0, playerHealth.RespawnTimer) : 0f;
}