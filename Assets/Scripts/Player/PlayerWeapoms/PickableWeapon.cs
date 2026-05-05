using Fusion;
using UnityEngine;

/// <summary>
/// Arma recogible en escena.
/// Puede estar colocada en el editor (no necesita ser spawneada por Fusion).
/// El pickup se sincroniza via el NetworkObject del jugador que la recoge.
/// </summary>
public class PickableWeapon : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius  = 2f;
    [SerializeField] private float pickupCooldown = 1f;

    [Header("Visual Feedback")]
    [SerializeField] private bool  enablePickupGlow = true;
    [SerializeField] private Color glowColor        = Color.yellow;
    [SerializeField] private float glowIntensity    = 1.5f;

    private BaseWeapon weaponComponent;
    private Collider   pickupCollider;
    private Renderer   weaponRenderer;
    private float      lastPickupTime = -999f;
    private bool       isPickedUp     = false;

    private void Start()
    {
        weaponComponent = GetComponent<BaseWeapon>();
        pickupCollider  = GetComponent<Collider>();
        weaponRenderer  = GetComponentInChildren<Renderer>();

        if (weaponComponent == null)
            Debug.LogWarning("[PickableWeapon] No hay BaseWeapon en este GameObject!");

        if (pickupCollider == null)
        {
            var sphere    = gameObject.AddComponent<SphereCollider>();
            sphere.radius = pickupRadius;
            sphere.isTrigger = true;
            pickupCollider   = sphere;
        }
        else
        {
            pickupCollider.isTrigger = true;
        }

        if (enablePickupGlow && weaponRenderer != null)
            ApplyGlowEffect();
    }

    private void Update()
    {
        if (isPickedUp) return;
        if (Time.time < lastPickupTime + pickupCooldown) return;

        CheckForNearbyPlayers();
    }

    private void CheckForNearbyPlayers()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius);

        foreach (Collider hit in hits)
        {
            PlayerWeaponManager weaponManager = hit.GetComponentInParent<PlayerWeaponManager>();
            if (weaponManager == null) continue;

            // Solo el objeto con StateAuthority ejecuta el pickup
            // (en Host Mode = el host)
            if (!weaponManager.Object.HasStateAuthority) continue;

            if (weaponManager.AddWeapon(weaponComponent))
            {
                isPickedUp     = true;
                lastPickupTime = Time.time;

                // Desactivar visualmente en todos los clientes via RPC
                // Como no somos NetworkBehaviour, usamos el runner del WeaponManager
                weaponManager.RPC_NotifyWeaponPickedUp(GetComponent<NetworkObject>() != null
                    ? GetComponent<NetworkObject>().Id
                    : default);

                // Desactivar localmente
                gameObject.SetActive(false);
                return;
            }
        }
    }

    public void Reset()
    {
        isPickedUp     = false;
        lastPickupTime = Time.time;
        if (pickupCollider != null) pickupCollider.enabled = true;
        gameObject.SetActive(true);
    }

    private void ApplyGlowEffect()
    {
        if (weaponRenderer == null) return;
        var mat = new Material(weaponRenderer.material);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", glowColor * glowIntensity);
        weaponRenderer.material = mat;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}