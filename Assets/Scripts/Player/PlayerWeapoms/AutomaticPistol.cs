using Fusion;
using UnityEngine;

public class AutomaticPistol : BaseWeapon
{
    
    public override void Spawned()
    {
        // ✅ Llamar base.Spawned() para garantizar visibilidad en clientes
        base.Spawned();

        if (Object.HasStateAuthority)
        {
            weaponName   = "Automatic Pistol";
            ammoCapacity = 20;
            fireRate     = 0.08f;
            reloadTime   = 1.5f;
            bulletSpeed  = 18f;
            CurrentAmmo = ammoCapacity;
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("[AutomaticPistol] Equipada!");
    }

    public override void Fire()
    {
        if (!Object.HasStateAuthority) return;
        if (isReloading || CurrentAmmo <= 0) { Reload(); return; }
        if (!firePoint) { Debug.LogError("[AutomaticPistol] firePoint null!"); return; }
        Shoot(Runner, firePoint.position, firePoint.forward, Object.InputAuthority, bulletPrefab);
    }
}