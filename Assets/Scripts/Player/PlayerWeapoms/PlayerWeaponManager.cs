using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponManager : NetworkBehaviour
{
    [Header("Weapon System")]
    [SerializeField] private int maxWeaponSlots = 3;
    [SerializeField] private Transform weaponHolderParent;

    // ✅ FIX: NetworkArray en lugar de List<> local para sincronización real en red
    [Networked, Capacity(3)]
    public NetworkArray<NetworkObject> WeaponSlots => default;

    // Lista local solo para referencia rápida en el cliente — se sincroniza desde WeaponSlots
    private List<BaseWeapon> weaponInventory = new List<BaseWeapon>();

    [Networked] public int CurrentWeaponSlot { get; set; }
    [Networked] public int InventoryCount { get; set; }

    // ✅ FIX: Guardar botones del tick anterior como [Networked] para WasPressed correcto
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    // ✅ FIX: Change detector para disparar eventos solo cuando cambia el estado de red
    private int _lastRenderedSlot = -1;
    private int _lastRenderedCount = -1;

    public delegate void OnWeaponChangeDelegate(BaseWeapon newWeapon, int slot);
    public delegate void OnInventoryChangeDelegate(int itemCount);
    public event OnWeaponChangeDelegate OnWeaponChanged;
    public event OnInventoryChangeDelegate OnInventoryChanged;

    public override void Spawned()
    {
        if (weaponHolderParent == null)
        {
            weaponHolderParent = transform.Find("WeaponHolder");
            if (weaponHolderParent == null)
            {
                GameObject holder = new GameObject("WeaponHolder");
                holder.transform.SetParent(transform);
                holder.transform.localPosition = Vector3.zero;
                weaponHolderParent = holder.transform;
            }
        }

        // Reconstruir inventario local desde el array de red al hacer spawn (reconexiones, etc.)
        RebuildLocalInventory();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            // ✅ FIX: GetPressed con los botones anteriores para detectar flanco ascendente
            var pressed = input.Buttons.GetPressed(PreviousButtons);
            PreviousButtons = input.Buttons;

            if (pressed.IsSet(PlayerButtons.NextWeapon))
                SwitchNextWeapon();

            if (pressed.IsSet(PlayerButtons.PrevWeapon))
                SwitchPreviousWeapon();
        }
    }

    // ✅ FIX: Render() para disparar eventos de UI — se ejecuta en todos los clientes
    public override void Render()
    {
        // Detectar cambio de arma equipada
        if (CurrentWeaponSlot != _lastRenderedSlot)
        {
            _lastRenderedSlot = CurrentWeaponSlot;
            OnWeaponChanged?.Invoke(GetCurrentWeapon(), CurrentWeaponSlot);
        }

        // Detectar cambio de inventario
        if (InventoryCount != _lastRenderedCount)
        {
            _lastRenderedCount = InventoryCount;
            OnInventoryChanged?.Invoke(InventoryCount);
        }
    }

    public bool AddWeapon(BaseWeapon weapon)
    {
        if (weapon == null) return false;

        if (weaponInventory.Count >= maxWeaponSlots)
        {
            Debug.LogWarning("[PlayerWeaponManager] Inventario lleno!");
            return false;
        }

        weaponInventory.Add(weapon);

        // ✅ Sincronizar también en el NetworkArray para que otros clientes lo vean
        NetworkObject netObj = weapon.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                if (WeaponSlots.Get(i) == null)
                {
                    WeaponSlots.Set(i, netObj);
                    break;
                }
            }
        }

        InventoryCount = weaponInventory.Count;

        if (weaponHolderParent != null)
        {
            weapon.transform.SetParent(weaponHolderParent);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }

        if (weaponInventory.Count == 1)
            EquipWeapon(0);

        // Nota: OnInventoryChanged se dispara en Render() para todos los clientes
        return true;
    }

    public void EquipWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponInventory.Count) return;

        foreach (var w in weaponInventory)
            if (w != null && w.IsEquipped()) w.OnUnequip();

        CurrentWeaponSlot = slotIndex;
        weaponInventory[slotIndex].OnEquip();

        // Nota: OnWeaponChanged se dispara en Render() para todos los clientes
    }

    public void SwitchNextWeapon()
    {
        if (weaponInventory.Count <= 1) return;
        EquipWeapon((CurrentWeaponSlot + 1) % weaponInventory.Count);
    }

    public void SwitchPreviousWeapon()
    {
        if (weaponInventory.Count <= 1) return;
        EquipWeapon((CurrentWeaponSlot - 1 + weaponInventory.Count) % weaponInventory.Count);
    }

    // ✅ FIX: RemoveWeapon ahora desequipa correctamente y re-equipa el siguiente slot válido
    public void RemoveWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponInventory.Count) return;

        BaseWeapon weaponToRemove = weaponInventory[slotIndex];

        // Desmontar del holder y desactivar
        if (weaponToRemove != null)
        {
            if (weaponToRemove.IsEquipped())
                weaponToRemove.OnUnequip();

            weaponToRemove.transform.SetParent(null);
        }

        // Limpiar del NetworkArray
        NetworkObject netObj = weaponToRemove != null ? weaponToRemove.GetComponent<NetworkObject>() : null;
        if (netObj != null)
        {
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                if (WeaponSlots.Get(i) == netObj)
                {
                    WeaponSlots.Set(i, null);
                    break;
                }
            }
        }

        weaponInventory.RemoveAt(slotIndex);
        InventoryCount = weaponInventory.Count;

        // Re-equipar el slot más cercano válido
        if (weaponInventory.Count > 0)
        {
            int nextSlot = Mathf.Clamp(slotIndex, 0, weaponInventory.Count - 1);
            EquipWeapon(nextSlot);
        }
        else
        {
            CurrentWeaponSlot = 0;
        }

        // Nota: OnInventoryChanged se dispara en Render()
    }

    public BaseWeapon GetCurrentWeapon() =>
        CurrentWeaponSlot >= 0 && CurrentWeaponSlot < weaponInventory.Count
            ? weaponInventory[CurrentWeaponSlot] : null;

    public List<BaseWeapon> GetInventory() => new List<BaseWeapon>(weaponInventory);
    public int GetWeaponCount() => weaponInventory.Count;

    // Reconstruye la lista local desde el NetworkArray (útil en reconexión o late-join)
    private void RebuildLocalInventory()
    {
        weaponInventory.Clear();
        for (int i = 0; i < WeaponSlots.Length; i++)
        {
            NetworkObject netObj = WeaponSlots.Get(i);
            if (netObj != null)
            {
                BaseWeapon weapon = netObj.GetComponent<BaseWeapon>();
                if (weapon != null)
                    weaponInventory.Add(weapon);
            }
        }
    }
}