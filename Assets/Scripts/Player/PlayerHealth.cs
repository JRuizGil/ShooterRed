using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de daño para el jugador
/// El personaje está compuesto de 9 cubos, y se elimina después de 4 impactos
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    [Header("Body Parts Setup")]
    [SerializeField] private GameObject[] bodyParts = new GameObject[9];
    [SerializeField] private Material damagedMaterial;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Feedback")]
    [SerializeField] private GameObject hitVfxPrefab;

    // Sincronizadas por red
    [Networked] public int HitsRemaining { get; set; } = 4;
    [Networked] public bool IsAlive { get; set; } = true;
    [Networked] public float RespawnTimer { get; set; } = 0f;

    private PlayerRef ownerPlayer;
    private PlayerState playerState;
    private List<GameObject> damagedParts = new List<GameObject>();
    private float localRespawnTimer = 0f;
    private bool isLocalPlayer = false;
    private HashSet<GameObject> hitParts = new HashSet<GameObject>();

    public override void Spawned()
    {
        Debug.Log($"[PlayerHealth] Spawned with {HitsRemaining} hits remaining");

        playerState = GetComponent<PlayerState>();
        if (playerState != null)
        {
            ownerPlayer = playerState.OwnerPlayer;
        }

        // Inicializar los cubos
        InitializeBodyParts();
        hitParts.Clear();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        // Manejar respawn
        if (!IsAlive && RespawnTimer > 0)
        {
            RespawnTimer -= Runner.DeltaTime;

            if (RespawnTimer <= 0)
            {
                RespawnPlayer();
            }
        }
    }

    /// <summary>
    /// Infligir daño al jugador en una parte específica
    /// </summary>
    public void TakeDamage(GameObject hitPart, PlayerRef attackerRef)
    {
        if (!IsAlive)
            return;

        if (!HasStateAuthority)
        {
            // Si no es el propietario, enviar RPC
            RPC_TakeDamage(hitPart.name, attackerRef);
            return;
        }

        // Procesar daño
        ProcessPartDamage(hitPart);

        // Reproducir VFX
        if (hitVfxPrefab != null)
        {
            Instantiate(hitVfxPrefab, hitPart.transform.position, Quaternion.identity);
        }

        // Notificar al atacante para kill tracking
        if (HitsRemaining <= 0)
        {
            IsAlive = false;
            RespawnTimer = respawnDelay;

            // Incrementar kill al atacante
            GameState gameState = FindFirstObjectByType<GameState>();
            if (gameState != null)
            {
                gameState.AddKill(attackerRef, ownerPlayer);
            }

            Debug.Log($"[PlayerHealth] Player {ownerPlayer} died! Respawning in {respawnDelay}s");
        }
    }

    /// <summary>
    /// Procesar daño a una parte del cuerpo
    /// </summary>
    private void ProcessPartDamage(GameObject hitPart)
    {
        if (hitPart == null)
            return;

        // Verificar si este cubo ya fue golpeado
        if (hitParts.Contains(hitPart))
        {
            Debug.LogWarning($"[PlayerHealth] Cubo {hitPart.name} ya fue golpeado, ignorando duplicado");
            return;
        }

        // Marcar como golpeado
        hitParts.Add(hitPart);

        // Cambiar material a "dañado"
        Renderer renderer = hitPart.GetComponent<Renderer>();
        if (renderer != null && damagedMaterial != null)
        {
            renderer.material = damagedMaterial;
        }

        damagedParts.Add(hitPart);
        HitsRemaining--;

        Debug.Log($"[PlayerHealth] Hit registered! {HitsRemaining} hits remaining");
    }

    /// <summary>
    /// Resucitar al jugador
    /// </summary>
    private void RespawnPlayer()
    {
        IsAlive = true;
        HitsRemaining = 4;
        RespawnTimer = 0f;
        damagedParts.Clear();
        hitParts.Clear();

        // Restaurar todos los cubos al material original
        foreach (GameObject bodyPart in bodyParts)
        {
            if (bodyPart != null)
            {
                Renderer renderer = bodyPart.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Restaurar al material original
                    renderer.material.color = Color.white;
                }
            }
        }

        // Reproducir respawn VFX
        PlayRespawnVFX();

        Debug.Log($"[PlayerHealth] Player {ownerPlayer} respawned!");
    }

    /// <summary>
    /// Resucitar al jugador (pública para RespawnManager)
    /// </summary>
    public void PublicRespawn()
    {
        RespawnPlayer();
    }

    /// <summary>
    /// Inicializar referencias de los cubos del cuerpo
    /// </summary>
    private void InitializeBodyParts()
    {
        if (bodyParts.Length == 0)
        {
            // Si no está configurado, intentar obtener automáticamente los cubos hijos
            bodyParts = new GameObject[GetComponentsInChildren<Renderer>().Length];
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                bodyParts[i] = renderers[i].gameObject;
            }

            Debug.LogWarning($"[PlayerHealth] Auto-detected {bodyParts.Length} body parts");
        }
    }

    /// <summary>
    /// Reproducir efecto visual de respawn
    /// </summary>
    private void PlayRespawnVFX()
    {
        // Aquí puedes agregar efectos visuales o sonoros de respawn
        Debug.Log("[PlayerHealth] Respawn VFX played");
    }

    /// <summary>
    /// RPC para sincronizar daño en todos los clientes
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_TakeDamage(string partName, PlayerRef attacker)
    {
        GameObject damagedPart = null;
        foreach (GameObject part in bodyParts)
        {
            if (part != null && part.name == partName)
            {
                damagedPart = part;
                break;
            }
        }

        if (damagedPart != null)
        {
            ProcessPartDamage(damagedPart);
        }
    }

    /// <summary>
    /// Getter para verificar si está vivo
    /// </summary>
    public bool GetIsAlive()
    {
        return IsAlive;
    }

    /// <summary>
    /// Getter para hits restantes
    /// </summary>
    public int GetHitsRemaining()
    {
        return HitsRemaining;
    }
}
