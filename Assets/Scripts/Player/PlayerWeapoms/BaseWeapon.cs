using Fusion;
using UnityEngine;

// Clase base de armas con soporte para disparo networked, munición infinita y recarga
public abstract class BaseWeapon : NetworkBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] protected string weaponName = "Weapon";
    [SerializeField] protected int ammoCapacity  = 30;  // Balas antes de recargar
    [SerializeField] protected float fireRate    = 0.1f; // Tiempo entre disparos
    [SerializeField] protected float reloadTime  = 1.5f; // Tiempo de recarga
    [SerializeField] protected float bulletSpeed = 20f;

    [Header("References")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected NetworkPrefabRef bulletPrefab;

    // Munición actual (resetea después de ammoCapacity balas)
    [Networked] public int CurrentAmmo { get; set; }
    
    // Temporizador para fuego y recarga
    [Networked] protected TickTimer FireCooldown { get; set; }
    [Networked] protected TickTimer ReloadCooldown { get; set; }
    [Networked] protected NetworkButtons PreviousButtons { get; set; }
    
    [Networked] private NetworkBool _isEquipped { get; set; }
    [Networked] private NetworkBool _isReloading { get; set; }

    protected bool isEquipped => _isEquipped;
    protected bool isReloading => _isReloading;

    public virtual string GetWeaponName() => weaponName;
    public virtual int GetCurrentAmmo()   => CurrentAmmo;
    public virtual int GetAmmoCapacity()  => ammoCapacity;
    public virtual bool IsEquipped()      => _isEquipped;
    public virtual bool IsReloading()     => _isReloading;

    // Puede disparar si: está equipada, no está recargando y cooldown de fuego expiró
    public virtual bool CanShoot() =>
        _isEquipped && !_isReloading && FireCooldown.ExpiredOrNotRunning(Runner);

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Verificar si la recarga terminó
        if (_isReloading && ReloadCooldown.Expired(Runner))
        {
            _isReloading = false;
            CurrentAmmo = ammoCapacity;
            Debug.Log($"[{weaponName}] ¡Recargada! Munición: {CurrentAmmo}");
        }
    }

    public virtual void OnEquip()
    {
        _isEquipped = true;
        CurrentAmmo = ammoCapacity;
        gameObject.SetActive(true);
        Debug.Log($"[{weaponName}] Equipada!");
    }

    public virtual void OnUnequip()
    {
        _isEquipped = false;
        Debug.Log($"[{weaponName}] Desequipada!");
    }

    public virtual void Reload()
    {
        if (!Object.HasStateAuthority || _isReloading) return;

        _isReloading = true;
        ReloadCooldown = TickTimer.CreateFromSeconds(Runner, reloadTime);
        Debug.Log($"[{weaponName}] Recargando...");
    }

    public virtual string GetAmmoDisplay()
    {
        if (_isReloading)
            return "RECARGANDO...";
        return $"{CurrentAmmo}/{ammoCapacity}";
    }

    public override void Render()
    {
        // Asegurar visibilidad cuando está equipada
        if (!_isEquipped) return;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    // Dispara una bala spawneada en red
    public virtual void Shoot(NetworkRunner runner, Vector3 origin, Vector3 direction,
                              PlayerRef shooter, NetworkPrefabRef externalBulletPrefab)
    {
        if (!CanShoot()) return;

        // Si la munición se agota, recargar automáticamente
        if (CurrentAmmo <= 1)
        {
            Reload();
            return;
        }

        FireCooldown = TickTimer.CreateFromSeconds(runner, fireRate);
        CurrentAmmo--;

        NetworkPrefabRef prefabToUse = bulletPrefab.IsValid ? bulletPrefab : externalBulletPrefab;
        if (!prefabToUse.IsValid)
        {
            Debug.LogError($"[{weaponName}] No hay bulletPrefab asignado!");
            return;
        }

        Vector3 velocity = direction.normalized * bulletSpeed;
        runner.Spawn(
            prefabToUse,
            origin,
            Quaternion.LookRotation(direction),
            shooter,
            onBeforeSpawned: (r, obj) => obj.GetComponent<NetworkBullet>()?.Init(velocity)
        );

        Debug.Log($"[{weaponName}] ¡Disparada! Munición: {CurrentAmmo}/{ammoCapacity}");
    }

    // Flujo alternativo: RPC para armas que lo necesiten
    protected virtual void HandleFireInput(PlayerNetworkInput input)
    {
        if (!_isEquipped || !Object.HasInputAuthority) return;

        bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Fire);
        PreviousButtons = input.Buttons;

        if (firePressed && CanShoot())
        {
            RPC_Fire();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    protected void RPC_Fire() => Fire();

    public abstract void Fire();
}