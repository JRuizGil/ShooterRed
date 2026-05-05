using Fusion;
using UnityEngine;

public class SemiPistol : BaseWeapon
{
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            weaponName = "Semi Pistol";
            ammoCapacity = 15;
            fireRate = 0.15f;
            bulletSpeed = 20f;
            CurrentAmmo = ammoCapacity;
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || !isEquipped) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            HandleFireInput(input); // usa el de BaseWeapon — ya tiene cooldown y WasPressed
        }
    }

    public override void Fire()
    {
        // Fire() siempre lo ejecuta el servidor vía RPC
        if (!Object.HasStateAuthority) return;
        if (CurrentAmmo <= 0)
        {
            Debug.Log("[SemiPistol] Sin munición!");
            return;
        }
        if (!bulletPrefab.IsValid)
        {
            Debug.LogError("[SemiPistol] bulletPrefab no registrado en NetworkProjectConfig!");
            return;
        }

        var bullet = Runner.Spawn(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation,
            Object.InputAuthority
        );

        bullet?.GetComponent<NetworkBullet>()?.Init(firePoint.forward * bulletSpeed);
        CurrentAmmo--;
        Debug.Log($"[SemiPistol] Disparada! Ammo: {CurrentAmmo}");
    }
}