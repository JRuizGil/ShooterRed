using Fusion;
using UnityEngine;

/// <summary>
/// Clase base para todas las armas del juego.
/// Define la interfaz común y comportamientos generales de las armas.
/// </summary>
public abstract class BaseWeapon : NetworkBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] protected string weaponName = "Weapon";
    [SerializeField] protected int ammoCapacity = 30;
    [SerializeField] protected float fireRate = 0.1f; // Segundos entre disparos
    [SerializeField] protected float bulletSpeed = 20f;

    [Header("References")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected NetworkPrefabRef bulletPrefab;

    // Networked properties
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] protected NetworkButtons PreviousButtons { get; set; }
    [Networked] protected TickTimer FireCooldown { get; set; }

    // Local variables
    
    protected bool isEquipped = false;
    protected bool canFire = true;

    public virtual string GetWeaponName() => weaponName;
    public virtual int GetCurrentAmmo() => CurrentAmmo;
    public virtual int GetAmmoCapacity() => ammoCapacity;
    public virtual bool IsEquipped() => isEquipped;

    /// <summary>
    /// Se llama cuando el arma es equipada por un jugador
    /// </summary>
    public virtual void OnEquip()
    {
        isEquipped = true;
        CurrentAmmo = ammoCapacity;
        gameObject.SetActive(true);
        Debug.Log($"[{weaponName}] Equipada!");
    }

    /// <summary>
    /// Se llama cuando el arma es desequipada
    /// </summary>
    public virtual void OnUnequip()
    {
        isEquipped = false;
        gameObject.SetActive(false);
        Debug.Log($"[{weaponName}] Desequipada!");
    }

    /// <summary>
    /// Recargar el arma
    /// </summary>
    public virtual void Reload()
    {
        if (!isEquipped) return;
        
        CurrentAmmo = ammoCapacity;
        Debug.Log($"[{weaponName}] Recargada! Munición: {CurrentAmmo}");
    }

    /// <summary>
    /// Obtener munición (para el HUD)
    /// </summary>
    public virtual string GetAmmoDisplay()
    {
        return $"{CurrentAmmo}/{ammoCapacity}";
    }

    /// <summary>
    /// Disparar arma (debe ser implementado por cada tipo de arma)
    /// </summary>
    public abstract void Fire();

    /// <summary>
    /// Método que se llama en FixedUpdateNetwork para manejar inputs
    /// </summary>
    // Añadir en BaseWeapon.cs — reemplaza HandleFireInput
    protected virtual void HandleFireInput(PlayerNetworkInput input)
    {
        if (!isEquipped || !Object.HasInputAuthority) return;

        bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Fire);
        PreviousButtons = input.Buttons;

        if (firePressed && FireCooldown.ExpiredOrNotRunning(Runner))
        {
            RPC_Fire(); // cliente pide al servidor
            FireCooldown = TickTimer.CreateFromSeconds(Runner, fireRate);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    protected void RPC_Fire()
    {
        Fire(); // servidor ejecuta el disparo real
    }
}
