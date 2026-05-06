using Fusion;
using UnityEngine;

public class SemiPistol : BaseWeapon
{
    
    public override void Spawned()
    {
        // ✅ Llamar base.Spawned() para garantizar visibilidad en clientes
        base.Spawned();

        if (Object.HasStateAuthority)
        {
            weaponName   = "Semi Pistol";
            ammoCapacity = 15;
            fireRate     = 0.15f;
            reloadTime   = 1.2f;
            bulletSpeed  = 20f;
            CurrentAmmo = ammoCapacity;
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("[SemiPistol] Equipada!");
    }

    public override void Fire()
    {
        if (!Object.HasStateAuthority) return;
        if (isReloading || CurrentAmmo <= 0) { Reload(); return; }
        if (!firePoint) { Debug.LogError("[SemiPistol] firePoint null!"); return; }
        Shoot(Runner, firePoint.position, firePoint.forward, Object.InputAuthority, bulletPrefab);
    }
}