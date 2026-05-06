using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponManager : NetworkBehaviour
{
    [Header("Weapon System")]
    [SerializeField] private int maxWeaponSlots = 3;
    [SerializeField] private Transform weaponHolderParent;

    [Header("Disparo")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private NetworkPrefabRef bulletPrefab;

    // Lista local de armas — no networked, se reconstruye desde InventoryCount
    private List<BaseWeapon> weaponInventory = new List<BaseWeapon>();

    [Networked] public int CurrentWeaponSlot { get; set; }
    [Networked] public int InventoryCount    { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    // Guardar referencias a armas por índice de forma simple
    // Usamos NetworkString para guardar el NetworkId de cada arma
    [Networked, Capacity(3)] public NetworkArray<NetworkId> WeaponIds => default;

    private int _lastRenderedSlot  = -1;
    private int _lastRenderedCount = -1;

    public delegate void OnWeaponChangeDelegate(BaseWeapon newWeapon, int slot);
    public delegate void OnInventoryChangeDelegate(int itemCount);
    public event OnWeaponChangeDelegate  OnWeaponChanged;
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
        RebuildLocalInventory();
    }

    // =========================================================
    // FIXED UPDATE
    // =========================================================
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            var pressed = input.Buttons.GetPressed(PreviousButtons);
            PreviousButtons = input.Buttons;

            if (pressed.IsSet(PlayerButtons.NextWeapon)) SwitchNextWeapon();
            if (pressed.IsSet(PlayerButtons.PrevWeapon)) SwitchPreviousWeapon();

            if (input.Buttons.IsSet(PlayerButtons.Fire))
                TryShoot(input);
        }
    }

    // =========================================================
    // RENDER
    // =========================================================
    public override void Render()
    {
        if (CurrentWeaponSlot != _lastRenderedSlot)
        {
            _lastRenderedSlot = CurrentWeaponSlot;
            UpdateWeaponVisibility();
            OnWeaponChanged?.Invoke(GetCurrentWeapon(), CurrentWeaponSlot);
        }

        if (InventoryCount != _lastRenderedCount)
        {
            _lastRenderedCount = InventoryCount;
            RebuildLocalInventory();
            UpdateWeaponVisibility();
            OnInventoryChanged?.Invoke(InventoryCount);
        }
    }

    // =========================================================
    // DISPARO
    // =========================================================
    private void TryShoot(PlayerNetworkInput input)
    {
        BaseWeapon weapon = GetCurrentWeapon();
        if (weapon == null || !weapon.IsEquipped() || !weapon.CanShoot()) return;

        Transform origin = firePoint != null ? firePoint : transform;
        weapon.Shoot(Runner, origin.position, origin.forward, Object.InputAuthority, bulletPrefab);
    }

    // =========================================================
    // AÑADIR ARMA
    // =========================================================
    public bool AddWeapon(BaseWeapon weapon)
    {
        if (weapon == null) return false;
        if (weaponInventory.Count >= maxWeaponSlots)
        {
            Debug.LogWarning("[PlayerWeaponManager] Inventario lleno!");
            return false;
        }

        // Verificar que no está ya en el inventario
        if (weaponInventory.Contains(weapon)) return false;

        weaponInventory.Add(weapon);

        // Guardar NetworkId si el arma tiene NetworkObject
        NetworkObject netObj = weapon.GetComponent<NetworkObject>();
        int slot = weaponInventory.Count - 1;
        if (netObj != null)
            WeaponIds.Set(slot, netObj.Id);

        InventoryCount = weaponInventory.Count;

        // Posicionar el arma en el WeaponHolder SIN usar SetParent en NetworkObjects
        PositionWeaponOnHolder(weapon);

        // Equipar automáticamente si es la primera arma
        if (weaponInventory.Count == 1)
            EquipWeapon(0);

        Debug.Log($"[PlayerWeaponManager] Arma añadida: {weapon.GetWeaponName()} slot:{slot}");
        return true;
    }

    // Posiciona visualmente el arma en el holder sin usar SetParent en NetworkObjects
    private void PositionWeaponOnHolder(BaseWeapon weapon)
    {
        if (weaponHolderParent == null) return;

        NetworkObject netObj = weapon.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            // NetworkObject: no se puede reparentar en Fusion
            // Usaremos FixedUpdate para que siga al holder
            // Registrar para seguimiento
            _trackedWeapons.Add(weapon);
        }
        else
        {
            // Sin NetworkObject: parentar normalmente
            weapon.transform.SetParent(weaponHolderParent);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }
    }

    // Lista de armas NetworkObject que siguen al holder
    private List<BaseWeapon> _trackedWeapons = new List<BaseWeapon>();

    private void LateUpdate()
    {
        if (weaponHolderParent == null) return;

        // Hacer que las armas NetworkObject sigan al WeaponHolder
        foreach (BaseWeapon weapon in _trackedWeapons)
        {
            if (weapon == null) continue;
            weapon.transform.position = weaponHolderParent.position;
            weapon.transform.rotation = weaponHolderParent.rotation;
        }
    }

    // =========================================================
    // VISIBILIDAD
    // =========================================================
    private void UpdateWeaponVisibility()
    {
        for (int i = 0; i < weaponInventory.Count; i++)
        {
            BaseWeapon w = weaponInventory[i];
            if (w == null) continue;

            if (i == CurrentWeaponSlot)
                w.OnEquip();
            else
                w.OnUnequip();
        }
    }

    // =========================================================
    // EQUIPAR / CAMBIAR
    // =========================================================
    public void EquipWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponInventory.Count) return;

        for (int i = 0; i < weaponInventory.Count; i++)
        {
            if (weaponInventory[i] == null) continue;
            if (i == slotIndex)
                weaponInventory[i].OnEquip();
            else
                weaponInventory[i].OnUnequip();
        }

        CurrentWeaponSlot = slotIndex;
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

    public void RemoveWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponInventory.Count) return;

        BaseWeapon toRemove = weaponInventory[slotIndex];
        if (toRemove != null)
        {
            if (toRemove.IsEquipped()) toRemove.OnUnequip();
            _trackedWeapons.Remove(toRemove);

            NetworkObject netObj = toRemove.GetComponent<NetworkObject>();
            if (netObj == null)
                toRemove.transform.SetParent(null);
        }

        WeaponIds.Set(slotIndex, default);
        weaponInventory.RemoveAt(slotIndex);
        InventoryCount = weaponInventory.Count;

        if (weaponInventory.Count > 0)
            EquipWeapon(Mathf.Clamp(slotIndex, 0, weaponInventory.Count - 1));
        else
            CurrentWeaponSlot = 0;
    }

    // =========================================================
    // GETTERS
    // =========================================================
    public BaseWeapon GetCurrentWeapon() =>
        CurrentWeaponSlot >= 0 && CurrentWeaponSlot < weaponInventory.Count
            ? weaponInventory[CurrentWeaponSlot] : null;

    public List<BaseWeapon> GetInventory() => new List<BaseWeapon>(weaponInventory);
    public int GetWeaponCount()            => weaponInventory.Count;

    // =========================================================
    // RECONSTRUIR INVENTARIO LOCAL desde WeaponIds networked
    // =========================================================
    private void RebuildLocalInventory()
    {
        weaponInventory.Clear();
        _trackedWeapons.Clear();

        for (int i = 0; i < WeaponIds.Length; i++)
        {
            NetworkId id = WeaponIds.Get(i);
            if (id == default) continue;

            NetworkObject netObj = Runner.FindObject(id);
            if (netObj == null) continue;

            BaseWeapon w = netObj.GetComponent<BaseWeapon>();
            if (w != null)
            {
                weaponInventory.Add(w);
                _trackedWeapons.Add(w);
            }
        }
    }

    // =========================================================
    // RPC para desactivar arma pickup en todos los clientes
    // =========================================================
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NotifyWeaponPickedUp(NetworkId weaponNetId)
    {
        if (weaponNetId == default) return;
        NetworkObject weaponObj = Runner.FindObject(weaponNetId);
        if (weaponObj != null)
            weaponObj.GetComponent<PickableWeapon>()?.gameObject.SetActive(false);
    }
}