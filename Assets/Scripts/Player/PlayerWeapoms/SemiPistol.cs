using Fusion;
using UnityEngine;

// Pistola semi-automática con disparo de un tiro
public class SemiPistol : BaseWeapon
{
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            weaponName = "Semi Pistol";
            ammoCapacity = 15;  // Balas antes de recargar
            fireRate = 0.15f;   // 150ms entre disparos
            reloadTime = 1.2f;  // Recarga rápida
            bulletSpeed = 20f;
            CurrentAmmo = ammoCapacity;
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("[SemiPistol] ¡Pistola semi-automática equipada!");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || !isEquipped) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            // Dispara al presionar el botón (un tiro por presión)
            bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Fire);
            PreviousButtons = input.Buttons;
            if (firePressed && CanShoot())
            {
                RPC_Fire();
            }
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
            Debug.LogError("[SemiPistol] bulletPrefab no registrado!");
            return;
        }

        // Disparar
        Shoot(Runner, firePoint.position, firePoint.forward, Object.InputAuthority, bulletPrefab);
    }
}