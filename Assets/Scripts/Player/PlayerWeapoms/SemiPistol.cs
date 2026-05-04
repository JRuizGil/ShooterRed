using Fusion;
using UnityEngine;

/// <summary>
/// Pistola semiautomática que dispara una bala por clic.
/// Ahora hereda de BaseWeapon para ser compatible con el sistema de pickups.
/// </summary>
public class SemiPistol : BaseWeapon
{
    private void Start()
    {
        weaponName = "Semi Pistol";
        ammoCapacity = 15;
        fireRate = 0.15f;
        bulletSpeed = 20f;
        CurrentAmmo = ammoCapacity;
    }

    public override void OnEquip()
    {
        base.OnEquip();
        // Aquí podrías añadir efectos de sonido, animaciones, etc.
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || !isEquipped) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            HandleFireInput(input);
        }
    }

    public override void Fire()
    {
        if (CurrentAmmo <= 0)
        {
            Debug.Log("[SemiPistol] ¡Sin munición!");
            return;
        }

        if (!Object.HasStateAuthority) return;

        RPC_RequestFire();
        CurrentAmmo--;
    }

    // El cliente pide disparar → Fusion enruta al servidor
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestFire()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("[SemiPistol] BulletPrefab no asignado!");
            return;
        }

        var bullet = Runner.Spawn(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation,
            Object.InputAuthority
        );

        NetworkBullet bulletScript = bullet.GetComponent<NetworkBullet>();
        if (bulletScript != null)
        {
            bulletScript.Init(firePoint.forward * bulletSpeed);
        }
    }
}
