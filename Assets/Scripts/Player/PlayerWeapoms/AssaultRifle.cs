using Fusion;
using UnityEngine;

// Rifle automático que dispara múltiples balas mientras se mantiene presionado
public class AssaultRifle : BaseWeapon
{
    [Header("Automatic Fire Settings")]
    [SerializeField] private float autoFireCooldown = 0.05f;
    [SerializeField] private bool useAutoFire = true;

    private float lastFireTime = 0f;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            weaponName = "Assault Rifle";
            ammoCapacity = 30;      // Balas antes de recargar
            fireRate = 0.05f;       // 50ms entre disparos
            reloadTime = 2f;        // Recarga más lenta
            bulletSpeed = 25f;
            CurrentAmmo = ammoCapacity;
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("[AssaultRifle] ¡Rifle automático equipado!");
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
            Debug.LogError("[AssaultRifle] bulletPrefab no registrado!");
            return;
        }

        // Disparar
        Shoot(Runner, firePoint.position, firePoint.forward, Object.InputAuthority, bulletPrefab);
    }
}
