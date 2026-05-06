using Fusion;
using UnityEngine;

// Pistola automática que hereda de BaseWeapon para funcionalidad networked
public class AutomaticPistol : BaseWeapon
{
    [Header("Automatic Fire Settings")]
    [SerializeField] private float autoFireCooldown = 0.08f;
    [SerializeField] private bool useAutoFire = true;

    private float lastFireTime = 0f;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            weaponName = "Automatic Pistol";
            ammoCapacity = 20;      // Balas antes de recargar
            fireRate = 0.08f;       // 80ms entre disparos
            reloadTime = 1.5f;      // Recarga media
            bulletSpeed = 18f;      // Velocidad media
            CurrentAmmo = ammoCapacity;
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("[AutomaticPistol] ¡Pistola automática equipada!");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || !isEquipped) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            if (useAutoFire)
            {
                HandleAutoFire(input);
            }
            else
            {
                HandleSemiAutoFire(input);
            }
        }
    }

    // Disparo automático: mientras se mantiene presionado el botón
    private void HandleAutoFire(PlayerNetworkInput input)
    {
        bool fireHeld = input.Buttons.IsSet(PlayerButtons.Fire);

        if (fireHeld && Time.time >= lastFireTime + autoFireCooldown && CanShoot())
        {
            RPC_Fire();
            lastFireTime = Time.time;
        }
    }

    // Disparo semi-automático: un tiro por presión
    private void HandleSemiAutoFire(PlayerNetworkInput input)
    {
        bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Fire);
        PreviousButtons = input.Buttons;
        if (firePressed && CanShoot())
        {
            RPC_Fire();
        }
    }

    public override void Fire()
    {
        // Solo el servidor ejecuta el disparo
        if (!Object.HasStateAuthority) return;

        // Si está recargando o no hay munición, no disparar
        if (isReloading || CurrentAmmo <= 0)
        {
            if (CurrentAmmo <= 0)
                Reload();
            return;
        }

        if (!bulletPrefab.IsValid)
        {
            Debug.LogError("[AutomaticPistol] bulletPrefab no registrado!");
            return;
        }

        // Disparar
        Shoot(Runner, firePoint.position, firePoint.forward, Object.InputAuthority, bulletPrefab);
    }
}
