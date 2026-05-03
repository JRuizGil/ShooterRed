using Fusion;
using UnityEngine;

/// <summary>
/// Sistema de armas del jugador
/// Maneja firing, raycast detection y comunicación de daño
/// </summary>
public class WeaponSystem : NetworkBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float weaponRange = 100f;
    [SerializeField] private LayerMask hitLayer;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera mainCamera;

    [Header("Effects")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject hitEffectPrefab;

    private float lastFireTime = 0f;
    private PlayerRef ownerPlayer;
    private PlayerHealth playerHealth;
    private bool isLocalPlayer = false;

    public override void Spawned()
    {
        Debug.Log($"[WeaponSystem] Weapon spawned");

        PlayerState playerState = GetComponent<PlayerState>();
        if (playerState != null)
        {
            ownerPlayer = playerState.OwnerPlayer;
        }

        playerHealth = GetComponent<PlayerHealth>();

        if (firePoint == null)
        {
            firePoint = transform;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Solo el propietario local necesita hacer raycast
        isLocalPlayer = HasInputAuthority;
    }

    private void Update()
    {
        // Solo el jugador local puede disparar
        if (!isLocalPlayer || !HasInputAuthority)
            return;

        if (Input.GetMouseButton(0))
        {
            Fire();
        }
    }

    /// <summary>
    /// Disparar
    /// </summary>
    private void Fire()
    {
        // Verificar cooldown
        if (Time.time - lastFireTime < fireRate)
            return;

        lastFireTime = Time.time;

        // Realizar raycast desde la cámara
        if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            ProcessShot(ray.origin, ray.direction);
        }
        else
        {
            // Alternativa: raycast desde el arma
            ProcessShot(firePoint.position, firePoint.forward);
        }

        // Reproducir muzzle flash local
        PlayMuzzleFlash();

        // Reproducir animación/sonido
        RPC_PlayFireEffects();
    }

    /// <summary>
    /// Procesar el disparo (raycast y daño)
    /// </summary>
    private void ProcessShot(Vector3 shootOrigin, Vector3 shootDirection)
    {
        RaycastHit hit;

        // Raycast desde la cámara hacia adelante
        if (Physics.Raycast(shootOrigin, shootDirection, out hit, weaponRange, hitLayer))
        {
            Debug.Log($"[WeaponSystem] Hit: {hit.collider.gameObject.name}");

            // Buscar PlayerHealth en el objeto golpeado o sus padres
            PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
            if (targetHealth == null)
            {
                targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            }

            if (targetHealth != null && targetHealth != playerHealth)
            {
                // Es un jugador enemigo
                targetHealth.TakeDamage(hit.collider.gameObject, ownerPlayer);

                // Reproducir hit effect
                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
            else if (targetHealth == playerHealth)
            {
                // No se puede dispararse a sí mismo
                Debug.LogWarning("[WeaponSystem] Cannot shoot yourself!");
                return;
            }

            // Dibujar línea de debug
            Debug.DrawLine(shootOrigin, hit.point, Color.red, 0.1f);
        }
        else
        {
            Debug.DrawLine(shootOrigin, shootOrigin + shootDirection * weaponRange, Color.green, 0.1f);
        }
    }

    /// <summary>
    /// Reproducir muzzle flash local
    /// </summary>
    private void PlayMuzzleFlash()
    {
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.2f);
        }
    }

    /// <summary>
    /// RPC para reproducir efectos de disparo en todos los clientes
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayFireEffects()
    {
        // Reproducir sonido de disparo
        // Reproducir animación de arma
        Debug.Log("[WeaponSystem] Fire effects played");
    }

    /// <summary>
    /// Configurar el layer de colisión para raycast
    /// </summary>
    public void SetHitLayer(LayerMask layer)
    {
        hitLayer = layer;
    }
}
