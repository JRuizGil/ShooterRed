using Fusion;
using UnityEngine;

// Escopeta que dispara múltiples balas en patrón de dispersión
public class Shotgun : BaseWeapon
{
    [Header("Shotgun Settings")]
    [SerializeField] private int pelletsPerShot = 8;      // Balas por disparo
    [SerializeField] private float spreadAngle = 15f;     // Ángulo de dispersión en grados
    [SerializeField] private float bulletSpeedVariation = 0.9f; // Variación de velocidad (0.9-1.1)

    private float lastFireTime = 0f;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            weaponName = "Shotgun";
            ammoCapacity = 8;       // Pocas balas antes de recargar
            fireRate = 0.8f;        // Lenta entre disparos
            reloadTime = 2.5f;      // Recarga muy lenta
            bulletSpeed = 22f;      // Velocidad media-alta
            CurrentAmmo = ammoCapacity;
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("[Shotgun] ¡Escopeta equipada!");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || !isEquipped) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            // Disparo semi-automático: un tiro por presión
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
            Debug.LogError("[Shotgun] bulletPrefab no registrado!");
            return;
        }

        // Establece el cooldown de fuego
        FireCooldown = TickTimer.CreateFromSeconds(Runner, fireRate);
        CurrentAmmo--;

        // Disparar múltiples balas en patrón de dispersión
        for (int i = 0; i < pelletsPerShot; i++)
        {
            // Calcular ángulos de dispersión aleatorios
            float randomSpreadX = Random.Range(-spreadAngle, spreadAngle);
            float randomSpreadY = Random.Range(-spreadAngle, spreadAngle);

            // Crear la dirección con dispersión
            Quaternion spreadRotation = Quaternion.Euler(randomSpreadX, randomSpreadY, 0);
            Vector3 spreadDirection = spreadRotation * firePoint.forward;

            // Variación de velocidad para cada pellet
            float speedVariation = Random.Range(bulletSpeedVariation, 1.1f);
            Vector3 velocity = spreadDirection.normalized * bulletSpeed * speedVariation;

            // Spawn de la bala
            Runner.Spawn(
                bulletPrefab,
                firePoint.position,
                Quaternion.LookRotation(spreadDirection),
                Object.InputAuthority,
                onBeforeSpawned: (r, obj) => obj.GetComponent<NetworkBullet>()?.Init(velocity)
            );
        }

        Debug.Log($"[Shotgun] ¡Disparada {pelletsPerShot} balas! Munición: {CurrentAmmo}/{ammoCapacity}");
    }
}
