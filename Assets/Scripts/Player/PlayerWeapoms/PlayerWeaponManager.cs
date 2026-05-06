using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponManager : NetworkBehaviour
{
    [Header("Weapon System")]
    [SerializeField] private int maxWeaponSlots = 3;
    [SerializeField] private Transform weaponHolderParent;

    [Header("Disparo")]
    [SerializeField] private Transform firePoint;       // fallback si el arma no tiene firePoint
    [SerializeField] private NetworkPrefabRef bulletPrefab;

    private List<BaseWeapon> weaponInventory = new List<BaseWeapon>();
    private List<BaseWeapon> _trackedWeapons = new List<BaseWeapon>();

    [Networked] public int CurrentWeaponSlot { get; set; }
    [Networked] public int InventoryCount    { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [Networked, Capacity(3)] public NetworkArray<NetworkId> WeaponIds => default;

    private int _lastRenderedSlot  = -1;
    private int _lastRenderedCount = -1;

    public delegate void OnWeaponChangeDelegate(BaseWeapon newWeapon, int slot);
    public delegate void OnInventoryChangeDelegate(int itemCount);
    public event OnWeaponChangeDelegate   OnWeaponChanged;
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
    // FIXED UPDATE — host gestiona input
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
    // RENDER — solo sincronización de estado networked
    // =========================================================
    public override void Render()
    {
        if (InventoryCount != _lastRenderedCount)
        {
            _lastRenderedCount = InventoryCount;
            RebuildLocalInventory();
            OnInventoryChanged?.Invoke(InventoryCount);
        }

        // Cambio de arma activa — sincronizado via CurrentWeaponSlot networked
        if (CurrentWeaponSlot != _lastRenderedSlot)
        {
            _lastRenderedSlot = CurrentWeaponSlot;
            UpdateWeaponVisibilityLocal();
            OnWeaponChanged?.Invoke(GetCurrentWeapon(), CurrentWeaponSlot);
        }
    }

    // =========================================================
    // LATE UPDATE — seguimiento local del arma al holder
    // Puramente visual, sin coste de red, se ejecuta en cada cliente
    // =========================================================
    private void LateUpdate()
    {
        TrackWeaponsToHolder();
    }

    // Actualizar visibilidad localmente basándose en CurrentWeaponSlot networked
    // Se ejecuta en TODOS los clientes desde Render()
    private void UpdateWeaponVisibilityLocal()
    {
        for (int i = 0; i < weaponInventory.Count; i++)
        {
            BaseWeapon w = weaponInventory[i];
            if (w == null) continue;

            bool shouldBeActive = (i == CurrentWeaponSlot);
            if (w.gameObject.activeSelf != shouldBeActive)
                w.gameObject.SetActive(shouldBeActive);
        }
    }

    // =========================================================
    // DISPARO
    // =========================================================
    // DISPARO
    // =========================================================
    private void TryShoot(PlayerNetworkInput input)
    {
        BaseWeapon weapon = GetCurrentWeapon();
        if (weapon == null || !weapon.IsEquipped() || !weapon.CanShoot()) return;

        // Usar el firePoint del arma si tiene uno asignado
        Transform fireTransform = weapon.GetFirePoint();
        if (fireTransform == null) fireTransform = firePoint;
        if (fireTransform == null) fireTransform = transform;

        // ✅ Usar la dirección del YawAngle del input para que coincida
        // con donde el jugador mira en su cliente — no la rotación local del host
        Vector3 shootDir = Quaternion.Euler(0, input.YawAngle, 0) * Vector3.forward;

        // Usar posición del fireTransform pero dirección del input
        Vector3 origin = fireTransform.position;

        weapon.Shoot(Runner, origin, shootDir, Object.InputAuthority, bulletPrefab);
    }

    // RPC eliminado — la bala networked spawneada es el efecto visual en todos los clientes
    // No hace falta RPC separado para efectos de disparo

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
        if (weaponInventory.Contains(weapon)) return false;

        int slot = weaponInventory.Count;
        weaponInventory.Add(weapon);

        NetworkObject netObj = weapon.GetComponent<NetworkObject>();
        if (netObj != null)
            WeaponIds.Set(slot, netObj.Id);

        InventoryCount = weaponInventory.Count;
        PositionWeaponOnHolder(weapon);

        if (weaponInventory.Count == 1)
            EquipWeapon(0);
        else
        {
            weapon.IsBeingCarried = true;
            weapon.gameObject.SetActive(false);
        }

        Debug.Log($"[PlayerWeaponManager] Arma añadida: {weapon.GetWeaponName()} slot:{slot}");
        return true;
    }

    private void PositionWeaponOnHolder(BaseWeapon weapon)
    {
        if (weaponHolderParent == null) return;

        NetworkObject netObj = weapon.GetComponent<NetworkObject>();
        if (netObj != null)
            _trackedWeapons.Add(weapon);
        else
        {
            weapon.transform.SetParent(weaponHolderParent);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }
    }

    private void TrackWeaponsToHolder()
    {
        if (weaponHolderParent == null) return;
        foreach (BaseWeapon weapon in _trackedWeapons)
        {
            if (weapon == null) continue;
            weapon.transform.position = weaponHolderParent.position;
            weapon.transform.rotation = weaponHolderParent.rotation;
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
            BaseWeapon w = weaponInventory[i];
            if (w == null) continue;
            if (i == slotIndex) w.OnEquip();
            else                w.OnUnequip();
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
            toRemove.IsBeingCarried = false;
            toRemove.gameObject.SetActive(true);
            _trackedWeapons.Remove(toRemove);
            if (toRemove.GetComponent<NetworkObject>() == null)
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
    // RECONSTRUIR INVENTARIO LOCAL
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

        // Actualizar visibilidad tras reconstruir
        UpdateWeaponVisibilityLocal();
    }

    // =========================================================
    // RPC pickup
    // =========================================================
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NotifyWeaponPickedUp(NetworkId weaponNetId)
    {
        if (weaponNetId == default) return;
        NetworkObject obj = Runner.FindObject(weaponNetId);
        obj?.GetComponent<PickableWeapon>()?.gameObject.SetActive(false);
    }
}