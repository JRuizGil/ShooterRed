using UnityEngine;

/// <summary>
/// Componente que hace que un arma sea recogible.
/// Se añade al mismo GameObject que el arma para permitir que los jugadores la recojan.
/// </summary>
public class PickableWeapon : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private float pickupCooldown = 1f;
    [SerializeField] private Vector3 pickupRotationOffset = Vector3.zero;

    [Header("Visual Feedback")]
    [SerializeField] private bool enablePickupGlow = true;
    [SerializeField] private Color glowColor = Color.yellow;
    [SerializeField] private float glowIntensity = 1.5f;

    private BaseWeapon weaponComponent;
    private Collider pickupCollider;
    private Renderer weaponRenderer;
    private Material originalMaterial;
    private float lastPickupTime = -999f;

    private void Start()
    {
        weaponComponent = GetComponent<BaseWeapon>();
        pickupCollider = GetComponent<Collider>();
        weaponRenderer = GetComponent<Renderer>();

        if (weaponComponent == null)
        {
            Debug.LogWarning("[PickableWeapon] No BaseWeapon encontrado en este GameObject!");
        }

        if (pickupCollider == null)
        {
            // Crear collider automáticamente si no existe
            pickupCollider = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)pickupCollider).radius = pickupRadius;
            pickupCollider.isTrigger = true;
            Debug.Log("[PickableWeapon] Collider creado automáticamente");
        }
        else
        {
            pickupCollider.isTrigger = true;
        }

        // Guardar material original para después
        if (weaponRenderer != null && enablePickupGlow)
        {
            originalMaterial = weaponRenderer.material;
            ApplyGlowEffect();
        }
    }

    /// <summary>
    /// Aplicar efecto de brillo al arma recogible
    /// </summary>
    private void ApplyGlowEffect()
    {
        if (weaponRenderer == null) return;

        // Crear material con glow
        Material glowMaterial = new Material(originalMaterial);
        glowMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);
        weaponRenderer.material = glowMaterial;
    }

    /// <summary>
    /// Verificar si un jugador está en rango de pickup y recoger si es posible
    /// </summary>
    private void Update()
    {
        // Verificar si hay jugadores en rango
        CheckForNearbyPlayers();
    }

    private void CheckForNearbyPlayers()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius);

        foreach (Collider col in colliders)
        {
            PlayerWeaponManager playerWeaponManager = col.GetComponent<PlayerWeaponManager>();
            if (playerWeaponManager != null)
            {
                // Verificar cooldown para evitar pickups múltiples
                if (Time.time >= lastPickupTime + pickupCooldown)
                {
                    TryPickup(playerWeaponManager);
                    lastPickupTime = Time.time;
                }
            }
        }
    }

    /// <summary>
    /// Intentar recoger el arma
    /// </summary>
    private void TryPickup(PlayerWeaponManager playerWeaponManager)
    {
        if (playerWeaponManager == null || weaponComponent == null)
            return;

        bool pickupSuccess = playerWeaponManager.AddWeapon(weaponComponent);

        if (pickupSuccess)
        {
            Debug.Log($"[PickableWeapon] {weaponComponent.GetWeaponName()} recogida por jugador!");
            // Desactivar solo el collider y este componente para que no se recoja de nuevo
            // El GameObject debe permanecer activo para que el arma funcione
            if (pickupCollider != null)
                pickupCollider.enabled = false;
            this.enabled = false;
        }
    }

    /// <summary>
    /// Resetear el arma (por ejemplo, cuando se respawnea)
    /// </summary>
    public void Reset()
    {
        if (pickupCollider != null)
            pickupCollider.enabled = true;
        this.enabled = true;
        lastPickupTime = Time.time;
        Debug.Log($"[PickableWeapon] {weaponComponent?.GetWeaponName() ?? "Weapon"} reseteada");
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar el radio de pickup en el editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
