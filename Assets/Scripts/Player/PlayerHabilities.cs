using Fusion;
using UnityEngine;

/// <summary>
/// Sistema de habilidades por rachas de bajas
/// 3 kills → Granada de pintura (Z)
/// 5 kills → Ataque aéreo (X)
/// 10 kills → Torreta de pintura (C)
/// </summary>
public class PlayerHabilities : NetworkBehaviour
{
    [Header("Ability Settings")]
    [SerializeField] private float grenadeRadius = 10f;
    [SerializeField] private int grenadeDamage = 30;
    
    [SerializeField] private float airStrikeDamage = 50;
    
    [SerializeField] private GameObject turretPrefab;
    [SerializeField] private float turretDuration = 15f;

    [Header("Key Bindings")]
    [SerializeField] private KeyCode grenadeKey = KeyCode.Z;
    [SerializeField] private KeyCode airStrikeKey = KeyCode.X;
    [SerializeField] private KeyCode turretKey = KeyCode.C;

    private PlayerRef ownerPlayer;
    private PlayerHealth playerHealth;
    private GameState gameState;

    // Estados de habilidades disponibles
    [Networked] public bool HasGrenade { get; set; }
    [Networked] public bool HasAirStrike { get; set; }
    [Networked] public bool HasTurret { get; set; }

    private void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        if (Input.GetKeyDown(grenadeKey) && HasGrenade)
        {
            RPC_ThrowGrenade();
        }

        if (Input.GetKeyDown(airStrikeKey) && HasAirStrike)
        {
            RPC_CallAirStrike();
        }

        if (Input.GetKeyDown(turretKey) && HasTurret)
        {
            RPC_SpawnTurret();
        }
    }

    public override void Spawned()
    {
        ownerPlayer = Object.InputAuthority;
        playerHealth = GetComponent<PlayerHealth>();
        gameState = FindFirstObjectByType<GameState>();

        Debug.Log($"[PlayerHabilities] Spawned for player {ownerPlayer}");
    }

    /// <summary>
    /// Llamado por GameState cuando el kill streak alcanza hitos
    /// </summary>
    public void UnlockAbility(int killStreak)
    {
        if (!HasStateAuthority)
            return;

        switch (killStreak)
        {
            case 3:
                HasGrenade = true;
                Debug.Log($"[PlayerHabilities] Player {ownerPlayer} unlocked GRENADE at 3 kills");
                break;
            case 5:
                HasAirStrike = true;
                Debug.Log($"[PlayerHabilities] Player {ownerPlayer} unlocked AIR STRIKE at 5 kills");
                break;
            case 10:
                HasTurret = true;
                Debug.Log($"[PlayerHabilities] Player {ownerPlayer} unlocked TURRET at 10 kills");
                break;
        }
    }

    /// <summary>
    /// Resetear habilidades al morir
    /// </summary>
    public void ResetAbilities()
    {
        if (!HasStateAuthority)
            return;

        HasGrenade = false;
        HasAirStrike = false;
        HasTurret = false;
    }

    // ========== GRANADA DE PINTURA ==========
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ThrowGrenade()
    {
        if (!HasGrenade)
            return;

        Vector3 spawnPos = transform.position + transform.forward * 2f + Vector3.up * 1.5f;
        Vector3 velocity = transform.forward * 15f + Vector3.up * 5f;

        // Crear la granada
        GameObject grenade = Instantiate(
            Resources.Load<GameObject>("NetworkPrefabs/GrenadePaintball"),
            spawnPos,
            Quaternion.identity
        );

        // Configurar física
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }

        // Configurar explosión
        GrenadePaintball grenadeScript = grenade.GetComponent<GrenadePaintball>();
        if (grenadeScript != null)
        {
            grenadeScript.Initialize(grenadeDamage, grenadeRadius, ownerPlayer);
        }

        HasGrenade = false;
        Debug.Log($"[PlayerHabilities] {ownerPlayer} threw grenade");
    }

    // ========== ATAQUE AÉREO ==========
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CallAirStrike()
    {
        if (!HasAirStrike)
            return;

        // Encontrar el enemigo más cercano
        PlayerState[] allPlayers = FindObjectsByType<PlayerState>(FindObjectsSortMode.None);
        PlayerState closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (PlayerState player in allPlayers)
        {
            if (player.OwnerPlayer == ownerPlayer || !player.GetComponent<PlayerHealth>().GetIsAlive())
                continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = player;
            }
        }
        

        if (closestEnemy != null)
        {
            Vector3 strikePos = closestEnemy.transform.position + Vector3.up * 50f;
            if (Physics.Raycast(strikePos, Vector3.down, out RaycastHit hit, 100f))
                strikePos = hit.point + Vector3.up * 0.5f;
            CreateAirStrikeEffect(strikePos);
            // TakeDamage recibe (string partName, PlayerRef attacker)
            // El air strike golpea todas las partes del cuerpo del enemigo
            PlayerHealth enemyHealth = closestEnemy.GetComponent<PlayerHealth>();
            if (enemyHealth != null && enemyHealth.GetIsAlive())
            {
                // Simular impacto en cada parte del cuerpo
                string[] airStrikeParts = { "Body_0", "Body_1", "Body_2", "Body_3" };
                foreach (string part in airStrikeParts)
                {
                    if (!enemyHealth.GetIsAlive()) break;
                        enemyHealth.TakeDamage(part, ownerPlayer);
                }
            }
        }

        HasAirStrike = false;
        Debug.Log($"[PlayerHabilities] {ownerPlayer} called air strike");
    }

    // ========== TORRETA ==========
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SpawnTurret()
    {
        if (!HasTurret || turretPrefab == null)
            return;

        Vector3 spawnPos = transform.position + transform.forward * 3f;

        GameObject turret = Instantiate(turretPrefab, spawnPos, Quaternion.identity);
        turret.name = $"Turret_{ownerPlayer}";

        // Configurar la torreta
        TurretStrike turretScript = turret.GetComponent<TurretStrike>();
        if (turretScript != null)
        {
            turretScript.Initialize(ownerPlayer, turretDuration);
        }

        HasTurret = false;
        Debug.Log($"[PlayerHabilities] {ownerPlayer} deployed turret");
    }

    // ========== EFECTOS VISUALES ==========
    private void CreateAirStrikeEffect(Vector3 position)
    {
        // Crear esfera visual de impacto
        GameObject effectSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effectSphere.transform.position = position;
        effectSphere.transform.localScale = Vector3.one * 5f;

        Collider col = effectSphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.5f, 0f, 0.5f);
        effectSphere.GetComponent<Renderer>().material = mat;

        Destroy(effectSphere, 0.3f);
    }
}

