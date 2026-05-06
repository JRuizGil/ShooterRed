using Fusion;
using UnityEngine;

public abstract class BaseWeapon : NetworkBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] protected string weaponName = "Weapon";
    [SerializeField] protected int ammoCapacity  = 30;
    [SerializeField] protected float fireRate    = 0.1f;
    [SerializeField] protected float reloadTime  = 1.5f;
    [SerializeField] protected float bulletSpeed = 20f;

    [Header("References")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected NetworkPrefabRef bulletPrefab;

    [Networked] public int CurrentAmmo { get; set; }
    [Networked] protected TickTimer FireCooldown { get; set; }
    [Networked] protected TickTimer ReloadCooldown { get; set; }
    [Networked] protected NetworkButtons PreviousButtons { get; set; }
    [Networked] private NetworkBool _isEquipped { get; set; }
    [Networked] private NetworkBool _isReloading { get; set; }
    [Networked] public NetworkBool IsBeingCarried { get; set; }

    protected bool isEquipped  => _isEquipped;
    protected bool isReloading => _isReloading;

    public virtual string GetWeaponName() => weaponName;
    public virtual int GetCurrentAmmo()   => CurrentAmmo;
    public virtual int GetAmmoCapacity()  => ammoCapacity;
    public virtual bool IsEquipped()      => _isEquipped;
    public virtual bool IsReloading()     => _isReloading;

    public virtual bool CanShoot() =>
        _isEquipped && !_isReloading && CurrentAmmo > 0 &&
        FireCooldown.ExpiredOrNotRunning(Runner);

    public override void FixedUpdateNetwork()
    {
        // Solo el host gestiona recarga
        if (!Object.HasStateAuthority) return;

        if (_isReloading && ReloadCooldown.Expired(Runner))
        {
            _isReloading = false;
            CurrentAmmo  = ammoCapacity;
            Debug.Log($"[{weaponName}] Recargada! Municion: {CurrentAmmo}");
        }
    }

    public virtual void OnEquip()
    {
        _isEquipped    = true;
        IsBeingCarried = true;
        CurrentAmmo    = ammoCapacity;
        gameObject.SetActive(true);
        Debug.Log($"[{weaponName}] Equipada!");
    }

    public virtual void OnUnequip()
    {
        _isEquipped = false;
        // IsBeingCarried sigue true — sigue en el inventario, solo no es el arma activa
        gameObject.SetActive(false);
        Debug.Log($"[{weaponName}] Desequipada!");
    }

    public virtual void Reload()
    {
        if (!Object.HasStateAuthority || _isReloading) return;
        _isReloading   = true;
        ReloadCooldown = TickTimer.CreateFromSeconds(Runner, reloadTime);
        Debug.Log($"[{weaponName}] Recargando...");
    }

    public virtual string GetAmmoDisplay()
    {
        if (_isReloading) return "RECARGANDO...";
        return $"{CurrentAmmo}/{ammoCapacity}";
    }

    public override void Spawned()
    {
        // El arma siempre visible al spawnearse (es un pickup en el suelo)
        // Se desactivará via OnEquip/OnUnequip cuando esté en inventario
        gameObject.SetActive(true);
    }

    public override void Render()
    {
        // Solo gestionar visibilidad si está siendo portada
        if (!IsBeingCarried) return;

        bool shouldBeActive = _isEquipped;
        if (gameObject.activeSelf != shouldBeActive)
            gameObject.SetActive(shouldBeActive);
    }

    // ✅ FIX 2: Exponer firePoint para que PlayerWeaponManager lo use
    // Garantiza que la bala sale desde la posición real del cañón del arma
    public Transform GetFirePoint() => firePoint;

    // ✅ FIX 3: Efecto visual de disparo en clientes remotos
    // Override en armas concretas para añadir flash, sonido, animación, etc.
    public virtual void OnFireEffect(Vector3 muzzlePos, Vector3 direction)
    {
        // Efecto por defecto: destello en el punto de disparo
        ImpactEffects.PlayImpactFlash(muzzlePos);
    }

    public virtual void Shoot(NetworkRunner runner, Vector3 origin, Vector3 direction,
                              PlayerRef shooter, NetworkPrefabRef externalBulletPrefab)
    {
        if (!CanShoot()) return;

        if (CurrentAmmo <= 0)
        {
            Reload();
            return;
        }

        FireCooldown = TickTimer.CreateFromSeconds(runner, fireRate);
        CurrentAmmo--;

        NetworkPrefabRef prefabToUse = bulletPrefab.IsValid ? bulletPrefab : externalBulletPrefab;
        if (!prefabToUse.IsValid)
        {
            Debug.LogError($"[{weaponName}] No hay bulletPrefab!");
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

        Debug.Log($"[{weaponName}] Disparada! Ammo: {CurrentAmmo}/{ammoCapacity}");
    }

    protected virtual void HandleFireInput(PlayerNetworkInput input)
    {
        if (!_isEquipped || !Object.HasInputAuthority) return;

        bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Fire);
        PreviousButtons  = input.Buttons;

        if (firePressed && CanShoot())
            RPC_Fire();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    protected void RPC_Fire() => Fire();

    public abstract void Fire();
}