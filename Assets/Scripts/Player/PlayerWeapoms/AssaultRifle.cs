using Fusion;
using UnityEngine;

public class AssaultRifle : BaseWeapon
{
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            weaponName   = "Assault Rifle";
            ammoCapacity = 30;
            fireRate     = 0.05f;
            reloadTime   = 2f;
            bulletSpeed  = 25f;
            CurrentAmmo  = ammoCapacity;
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("[AssaultRifle] Equipado!");
    }

    // ✅ Sin FixedUpdateNetwork propio — PlayerWeaponManager gestiona el disparo
    public override void Fire()
    {
        if (!Object.HasStateAuthority) return;
        if (isReloading || CurrentAmmo <= 0) { Reload(); return; }
        if (!firePoint) { Debug.LogError("[AssaultRifle] firePoint null!"); return; }
        Shoot(Runner, firePoint.position, firePoint.forward, Object.InputAuthority, bulletPrefab);
    }
}