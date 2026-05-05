using Fusion;
using UnityEngine;

public abstract class BaseWeapon : NetworkBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] protected string weaponName = "Weapon";
    [SerializeField] protected int ammoCapacity  = 30;
    [SerializeField] protected float fireRate    = 0.1f;
    [SerializeField] protected float bulletSpeed = 20f;

    [Header("References")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected NetworkPrefabRef bulletPrefab;

    [Networked] public int CurrentAmmo { get; set; }
    [Networked] protected NetworkButtons PreviousButtons { get; set; }
    [Networked] protected TickTimer FireCooldown { get; set; }

    // isEquipped networked — todos los clientes ven el estado correcto
    [Networked] private NetworkBool _isEquipped { get; set; }

    // Propiedad protegida para que las clases hijas puedan leerla igual que antes
    protected bool isEquipped => _isEquipped;

    public virtual string GetWeaponName() => weaponName;
    public virtual int GetCurrentAmmo()   => CurrentAmmo;
    public virtual int GetAmmoCapacity()  => ammoCapacity;
    public virtual bool IsEquipped()      => _isEquipped;

    // CanShoot publico para que PlayerWeaponManager lo consulte
    public virtual bool CanShoot() =>
        _isEquipped && CurrentAmmo > 0 && FireCooldown.ExpiredOrNotRunning(Runner);

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
        // No desactivar el GameObject aquí — el WeaponHolder lo gestiona
        // Si desactivamos, el objeto desaparece también como pickup
        Debug.Log($"[{weaponName}] Desequipada!");
    }

    public virtual void Reload()
    {
        if (!_isEquipped) return;
        CurrentAmmo = ammoCapacity;
    }

    public virtual string GetAmmoDisplay() => $"{CurrentAmmo}/{ammoCapacity}";

    public override void Render()
    {
        // Solo gestionar visibilidad cuando el arma está siendo portada por un jugador
        // Si no está equipada por nadie (pickup en suelo) no tocar el GameObject
        if (!_isEquipped) return;

        // Si está equipada, asegurarse de que sea visible
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    // =========================================================
    // RUTA A: PlayerWeaponManager llama Shoot() desde el host
    // =========================================================
    public virtual void Shoot(NetworkRunner runner, Vector3 origin, Vector3 direction,
                              PlayerRef shooter, NetworkPrefabRef externalBulletPrefab)
    {
        if (!CanShoot()) return;

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

        Debug.Log($"[{weaponName}] Disparada! Ammo: {CurrentAmmo}");
    }

    // =========================================================
    // RUTA B: cliente llama HandleFireInput -> RPC al host
    // (mantener por compatibilidad con armas que lo usen directamente)
    // =========================================================
    protected virtual void HandleFireInput(PlayerNetworkInput input)
    {
        if (!_isEquipped || !Object.HasInputAuthority) return;

        bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Fire);
        PreviousButtons  = input.Buttons;

        if (firePressed && FireCooldown.ExpiredOrNotRunning(Runner))
        {
            RPC_Fire();
            FireCooldown = TickTimer.CreateFromSeconds(Runner, fireRate);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    protected void RPC_Fire() => Fire();

    public abstract void Fire();
}