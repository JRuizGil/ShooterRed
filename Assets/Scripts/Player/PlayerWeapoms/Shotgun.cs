using Fusion;
using UnityEngine;

public class Shotgun : BaseWeapon
{
    [Header("Shotgun Settings")]
    [SerializeField] private int pelletsPerShot = 8;
    [SerializeField] private float spreadAngle = 15f;
    [SerializeField] private float bulletSpeedVariation = 0.9f;
    public override void Spawned()
    {
        // ✅ Llamar base.Spawned() para garantizar visibilidad en clientes
        base.Spawned();

        if (Object.HasStateAuthority)
        {
            weaponName   = "Shotgun";
            ammoCapacity = 8;
            fireRate     = 0.8f;
            reloadTime   = 2.5f;
            bulletSpeed  = 22f;
            CurrentAmmo = ammoCapacity;
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("[Shotgun] Equipada!");
    }

    public override void Fire()
    {
        if (!Object.HasStateAuthority) return;
        if (isReloading || CurrentAmmo <= 0) { Reload(); return; }
        if (!firePoint) { Debug.LogError("[Shotgun] firePoint null!"); return; }

        if (!bulletPrefab.IsValid) { Debug.LogError("[Shotgun] bulletPrefab no asignado!"); return; }
        FireCooldown = TickTimer.CreateFromSeconds(Runner, fireRate);
        CurrentAmmo--;
        for (int i = 0; i < pelletsPerShot; i++)
        {
            float rX = Random.Range(-spreadAngle, spreadAngle);
            float rY = Random.Range(-spreadAngle, spreadAngle);
            Vector3 dir     = Quaternion.Euler(rX, rY, 0) * firePoint.forward;
            float speedMult = Random.Range(bulletSpeedVariation, 1.1f);
            Vector3 vel     = dir.normalized * bulletSpeed * speedMult;
            Runner.Spawn(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir),
                Object.InputAuthority,
                onBeforeSpawned: (r, obj) => obj.GetComponent<NetworkBullet>()?.Init(vel));
        }
        Debug.Log($"[Shotgun] {pelletsPerShot} balas! Ammo:{CurrentAmmo}/{ammoCapacity}");
    }
}