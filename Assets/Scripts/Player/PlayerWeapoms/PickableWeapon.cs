using Fusion;
using UnityEngine;

public class PickableWeapon : NetworkBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius   = 2f;
    [SerializeField] private float pickupCooldown = 1f;

    [Header("Visual")]
    [SerializeField] private bool  enableGlow    = true;
    [SerializeField] private Color glowColor     = Color.yellow;
    [SerializeField] private float glowIntensity = 1.5f;

    // Networked: cuando cambia a true, todos los clientes desactivan el pickup visual
    [Networked, OnChangedRender(nameof(OnPickupStateChanged))]
    public NetworkBool IsPickedUp { get; set; }

    private BaseWeapon weaponComponent;
    private Renderer   weaponRenderer;
    private Collider   pickupCollider;
    private float      lastCheckTime = -999f;

    public override void Spawned()
    {
        weaponComponent = GetComponent<BaseWeapon>();
        weaponRenderer  = GetComponentInChildren<Renderer>(true);

        pickupCollider = GetComponent<Collider>();
        if (pickupCollider == null)
        {
            var sphere       = gameObject.AddComponent<SphereCollider>();
            sphere.radius    = pickupRadius;
            sphere.isTrigger = true;
            pickupCollider   = sphere;
        }
        else
        {
            pickupCollider.isTrigger = true;
        }

        if (weaponComponent == null)
            Debug.LogWarning($"[PickableWeapon] Sin BaseWeapon en {name}");

        if (enableGlow && weaponRenderer != null)
            ApplyGlow();

        // Si ya fue recogida (cliente tardío), desactivar visualmente
        if (IsPickedUp)
            DisablePickupVisuals();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (IsPickedUp) return;
        if (Runner.SimulationTime < lastCheckTime + pickupCooldown) return;

        lastCheckTime = Runner.SimulationTime;

        var hits = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (var hit in hits)
        {
            PlayerWeaponManager wm = hit.GetComponentInParent<PlayerWeaponManager>();
            if (wm == null || weaponComponent == null) continue;

            if (wm.AddWeapon(weaponComponent))
            {
                // ✅ NO despawnear — marcar como recogida
                // El arma sigue existiendo como NetworkObject en el inventario
                IsPickedUp = true;
                return;
            }
        }
    }

    // OnChangedRender se ejecuta en TODOS los clientes cuando IsPickedUp cambia
    private void OnPickupStateChanged()
    {
        if (IsPickedUp)
            DisablePickupVisuals();
    }

    private void DisablePickupVisuals()
    {
        // Desactivar solo el collider de pickup — el modelo lo gestiona BaseWeapon.Render()
        if (pickupCollider != null)
            pickupCollider.enabled = false;

        // Quitar el glow si está aplicado
        if (weaponRenderer != null && weaponRenderer.material.HasProperty("_EmissionColor"))
        {
            weaponRenderer.material.DisableKeyword("_EMISSION");
            weaponRenderer.material.SetColor("_EmissionColor", Color.black);
        }
    }

    private void ApplyGlow()
    {
        Material mat = weaponRenderer.material;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * glowIntensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}