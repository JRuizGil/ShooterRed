using Fusion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor de inventario de armas del jugador.
/// Sincroniza las armas equipadas entre todos los clientes.
/// </summary>
public class PlayerWeaponManager : NetworkBehaviour
{
    [Header("Weapon System")]
    [SerializeField] private int maxWeaponSlots = 3;
    [SerializeField] private Transform weaponHolderParent; // Parent para las armas activas
    [SerializeField] private PlayerNetworkInput playerNetworkInput; // Referencia para enviar input

    // Armas del inventario
    private List<BaseWeapon> weaponInventory = new List<BaseWeapon>();
    
    [Networked] public int CurrentWeaponSlot { get; set; } = 0;
    [Networked] public int InventoryCount { get; set; } = 0;

    // Eventos
    public delegate void OnWeaponChangeDelegate(BaseWeapon newWeapon, int slot);
    public delegate void OnInventoryChangeDelegate(int itemCount);

    public event OnWeaponChangeDelegate OnWeaponChanged;
    public event OnInventoryChangeDelegate OnInventoryChanged;

    private void Start()
    {
        if (weaponHolderParent == null)
        {
            weaponHolderParent = transform.Find("WeaponHolder");
            if (weaponHolderParent == null)
            {
                // Crear GameObject automáticamente
                GameObject holder = new GameObject("WeaponHolder");
                holder.transform.SetParent(transform);
                holder.transform.localPosition = Vector3.zero;
                weaponHolderParent = holder.transform;
            }
        }
        
        
    }
    

    /// <summary>
    /// Añadir un arma al inventario
    /// </summary>
    public bool AddWeapon(BaseWeapon weapon)
    {
        if (weaponInventory.Count >= maxWeaponSlots)
        {
            Debug.LogWarning("[PlayerWeaponManager] Inventario lleno!");
            return false;
        }

        if (weapon == null)
        {
            Debug.LogWarning("[PlayerWeaponManager] Intento de añadir un arma nula!");
            return false;
        }

        // Añadir a inventario
        weaponInventory.Add(weapon);
        InventoryCount = weaponInventory.Count;

        // Reparentar el arma
        if (weaponHolderParent != null)
        {
            weapon.transform.SetParent(weaponHolderParent);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }

        // Si es la primera arma, equiparla automáticamente
        if (weaponInventory.Count == 1)
        {
            EquipWeapon(0);
        }

        Debug.Log($"[PlayerWeaponManager] Arma añadida: {weapon.GetWeaponName()}. Total en inventario: {weaponInventory.Count}");
        OnInventoryChanged?.Invoke(weaponInventory.Count);

        return true;
    }

    /// <summary>
    /// Equipar un arma del inventario
    /// </summary>
    public void EquipWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponInventory.Count)
        {
            Debug.LogWarning($"[PlayerWeaponManager] Slot inválido: {slotIndex}");
            return;
        }

        // Desequipar arma anterior
        for (int i = 0; i < weaponInventory.Count; i++)
        {
            if (i != slotIndex && weaponInventory[i].IsEquipped())
            {
                weaponInventory[i].OnUnequip();
            }
        }

        // Equipar nueva arma
        CurrentWeaponSlot = slotIndex;
        BaseWeapon currentWeapon = weaponInventory[slotIndex];
        currentWeapon.OnEquip();

        Debug.Log($"[PlayerWeaponManager] Arma equipada: {currentWeapon.GetWeaponName()}");
        OnWeaponChanged?.Invoke(currentWeapon, slotIndex);
    }

    /// <summary>
    /// Cambiar a la siguiente arma
    /// </summary>
    public void SwitchNextWeapon()
    {
        if (weaponInventory.Count <= 1)
        {
            Debug.LogWarning("[PlayerWeaponManager] Solo hay 0-1 arma en inventario");
            return;
        }

        int nextSlot = (CurrentWeaponSlot + 1) % weaponInventory.Count;
        Debug.Log($"[PlayerWeaponManager] Cambiando de slot {CurrentWeaponSlot} a {nextSlot}");
        EquipWeapon(nextSlot);
    }

    /// <summary>
    /// Cambiar a la arma anterior
    /// </summary>
    public void SwitchPreviousWeapon()
    {
        if (weaponInventory.Count <= 1)
        {
            Debug.LogWarning("[PlayerWeaponManager] Solo hay 0-1 arma en inventario");
            return;
        }

        int prevSlot = (CurrentWeaponSlot - 1 + weaponInventory.Count) % weaponInventory.Count;
        Debug.Log($"[PlayerWeaponManager] Cambiando de slot {CurrentWeaponSlot} a {prevSlot}");
        EquipWeapon(prevSlot);
    }

    /// <summary>
    /// Obtener el arma actualmente equipada
    /// </summary>
    public BaseWeapon GetCurrentWeapon()
    {
        if (CurrentWeaponSlot >= 0 && CurrentWeaponSlot < weaponInventory.Count)
        {
            return weaponInventory[CurrentWeaponSlot];
        }
        return null;
    }

    /// <summary>
    /// Obtener el inventario completo
    /// </summary>
    public List<BaseWeapon> GetInventory()
    {
        return new List<BaseWeapon>(weaponInventory);
    }

    /// <summary>
    /// Obtener cantidad de armas en el inventario
    /// </summary>
    public int GetWeaponCount()
    {
        return weaponInventory.Count;
    }

    /// <summary>
    /// Remover un arma del inventario
    /// </summary>
    public void RemoveWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponInventory.Count) return;

        BaseWeapon removedWeapon = weaponInventory[slotIndex];
        weaponInventory.RemoveAt(slotIndex);
        InventoryCount = weaponInventory.Count;

        if (CurrentWeaponSlot >= weaponInventory.Count)
        {
            CurrentWeaponSlot = Mathf.Max(0, weaponInventory.Count - 1);
        }

        Debug.Log($"[PlayerWeaponManager] Arma removida: {removedWeapon.GetWeaponName()}");
        OnInventoryChanged?.Invoke(weaponInventory.Count);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority) return;

        // Manejar cambio de armas con números
        if (Input.GetKeyDown(KeyCode.E))
            SwitchNextWeapon();

        if (Input.GetKeyDown(KeyCode.Q))
            SwitchPreviousWeapon();

        // Las armas manejan su propio input en sus FixedUpdateNetwork
        playerNetworkInput = GetComponent<PlayerNetworkInput>();
    }
}
