using Fusion;
using UnityEngine;

/// <summary>
/// Sistema de torreta automática sincronizada en red
/// La torreta apunta y dispara automáticamente a los enemigos
/// </summary>
public class TurretSystem : NetworkBehaviour
{
    [Header("Turret Settings")]
    [SerializeField] private float detectionRange = 30f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("References")]
    [SerializeField] private Transform barrelTransform;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Effects")]
    [SerializeField] private GameObject muzzleFlashPrefab;

    // Variables de sincronización
    [Networked] public NetworkObject CurrentTarget { get; set; }
    [Networked] public float NextFireTime { get; set; } = 0f;

    private float fireTimer = 0f;
    private Collider[] detectionResults = new Collider[10];

    public override void Spawned()
    {
        Debug.Log("[TurretSystem] Turret spawned");

        if (firePoint == null)
            firePoint = transform;

        if (barrelTransform == null)
            barrelTransform = transform;

        NextFireTime = Time.time;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        // Buscar objetivo más cercano
        FindNearestTarget();

        // Apuntar al objetivo
        if (CurrentTarget != null)
        {
            AimAtTarget();
            CheckFire();
        }
        else
        {
            // Retornar a posición neutral si no hay objetivo
            RotateTowards(barrelTransform.forward + Vector3.up * 0.5f);
        }
    }

    /// <summary>
    /// Buscar el objetivo enemigo más cercano
    /// </summary>
    private void FindNearestTarget()
    {
        int hits = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, detectionResults, enemyLayer);

        NetworkObject closestTarget = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits; i++)
        {
            if (detectionResults[i] == null)
                continue;

            Collider col = detectionResults[i];
            PlayerHealth playerHealth = col.GetComponent<PlayerHealth>();
            
            if (playerHealth != null && playerHealth.GetIsAlive())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = col.GetComponent<NetworkObject>();
                }
            }
        }

        CurrentTarget = closestTarget;
    }

    /// <summary>
    /// Apuntar hacia el objetivo
    /// </summary>
    private void AimAtTarget()
    {
        if (CurrentTarget == null)
            return;

        Vector3 directionToTarget = (CurrentTarget.transform.position - barrelTransform.position).normalized;
        RotateTowards(directionToTarget);
    }

    /// <summary>
    /// Rotar el barrel hacia una dirección
    /// </summary>
    private void RotateTowards(Vector3 direction)
    {
        if (barrelTransform == null)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        barrelTransform.rotation = Quaternion.RotateTowards(
            barrelTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// Verificar y ejecutar disparo
    /// </summary>
    private void CheckFire()
    {
        if (Time.time >= NextFireTime)
        {
            Fire();
            NextFireTime = Time.time + fireRate;
        }
    }

    /// <summary>
    /// Disparar
    /// </summary>
    private void Fire()
    {
        if (CurrentTarget == null)
            return;

        Debug.Log("[TurretSystem] Turret firing!");

        // Reproducir efectos visuales
        PlayFireEffects();

        // Si hay un prefab de proyectil, instanciarlo
        if (projectilePrefab != null && firePoint != null)
        {
            Vector3 targetPos = CurrentTarget.transform.position;
            Vector3 directionToTarget = (targetPos - firePoint.position).normalized;
            
            GameObject projectile = Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(directionToTarget)
            );

            TurretProjectile proj = projectile.GetComponent<TurretProjectile>();
            if (proj != null)
            {
                proj.Initialize(directionToTarget, this);
            }
        }
        else
        {
            // Alternative: raycast directo (más simple)
            if (firePoint != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, detectionRange))
                {
                    PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
                    if (targetHealth != null)
                    {
                            // Usar PlayerRef.None para la torreta
                            targetHealth.TakeDamage(hit.collider.gameObject, PlayerRef.None);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reproducir efectos visuales de disparo
    /// </summary>
    private void PlayFireEffects()
    {
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.15f);
        }
    }

    /// <summary>
    /// RPC para sincronizar efectos de disparo
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayTurretFireEffects()
    {
        // Reproducir sonido de disparo
        // Reproducir animación de retroceso
        Debug.Log("[TurretSystem] Fire effects sync");
    }

    /// <summary>
    /// Dibujar gizmos para debug
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}

/// <summary>
/// Proyectil de la torreta
/// </summary>
public class TurretProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 10f;
    private Vector3 direction;
    private TurretSystem turret;
    private float spawnTime;

    public void Initialize(Vector3 fireDirection, TurretSystem turretSystem)
    {
        direction = fireDirection;
        turret = turretSystem;
        spawnTime = Time.time;
    }

    private void Update()
    {
        // Mover el proyectil
        transform.position += direction * speed * Time.deltaTime;

        // Auto-destruir después de cierto tiempo
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
        }

        // Raycast para detectar colisiones
        RaycastHit hit;
        if (Physics.Raycast(transform.position - direction * speed * Time.deltaTime, direction, out hit, speed * Time.deltaTime * 2))
        {
            if (!hit.collider.CompareTag("Turret"))
            {
                PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
                if (targetHealth != null)
                {
                    // Usar PlayerRef.None para la torreta
                    targetHealth.TakeDamage(hit.collider.gameObject, PlayerRef.None);
                }

                Destroy(gameObject);
            }
        }
    }
}
