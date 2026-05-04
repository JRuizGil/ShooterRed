using Fusion;
using UnityEngine;

/// <summary>
/// Rifle Automático - Ejemplo de arma que hereda de BaseWeapon.
/// Dispara múltiples balas mientras se mantiene presionado el botón.
/// </summary>
public class AssaultRifle : BaseWeapon
{
    [Header("Automatic Fire Settings")]
    [SerializeField] private float autoFireCooldown = 0.05f;
    [SerializeField] private bool useAutoFire = true;

    private float lastFireTime = 0f;

    private void Start()
    {
        weaponName = "Assault Rifle";
        ammoCapacity = 30;
        fireRate = 0.05f; // Muy rápido para automático
        bulletSpeed = 25f;
        CurrentAmmo = ammoCapacity;
    }

    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log($"[{weaponName}] ¡Rifle automático equipado!");
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
                HandleFireInput(input);
            }
        }
    }

    private void HandleAutoFire(PlayerNetworkInput input)
    {
        // Fuego automático: dispara mientras se mantenga presionado
        bool fireHeld = input.Buttons.IsSet(PlayerButtons.Fire);
        
        if (fireHeld)
        {
            if (Time.time >= lastFireTime + autoFireCooldown && CurrentAmmo > 0)
            {
                Fire();
                lastFireTime = Time.time;
            }
        }
    }

    public override void Fire()
    {
        if (CurrentAmmo <= 0)
        {
            Debug.Log($"[{weaponName}] ¡Sin munición!");
            return;
        }

        if (!Object.HasStateAuthority) return;

        RPC_RequestFire();
        CurrentAmmo--;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestFire()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError($"[{weaponName}] BulletPrefab no asignado!");
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

    public override void Reload()
    {
        base.Reload();
        lastFireTime = Time.time; // Reset cooldown al recargar
    }
}
