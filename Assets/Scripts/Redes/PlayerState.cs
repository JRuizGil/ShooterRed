using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Networked] public PlayerRef OwnerPlayer { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

    [SerializeField] private GameObject hitVfx;

    public override void Spawned()
    {
        OwnerPlayer = Object.InputAuthority;

        // ✅ Registrar en PlayerRegistry para lookup rapido
        PlayerRegistry.Register(OwnerPlayer, Object);

        Debug.Log($"[PlayerState] Spawned: {OwnerPlayer}");
        RefreshHealthVisuals();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // ✅ Limpiar del registro al despawnear
        PlayerRegistry.Unregister(OwnerPlayer);
        Debug.Log($"[PlayerState] Despawned: {OwnerPlayer}");

        if (hitVfx != null)
            hitVfx.SetActive(false);
    }

    private void OnHealthChanged()   => RefreshHealthVisuals();
    private void OnPlayerNameChanged() => Debug.Log($"[PlayerState] Nombre: {PlayerName}");

    private void RefreshHealthVisuals()
    {
        // Conectar con GameHUD si es el jugador local
        if (Object.HasInputAuthority && GameHUD.Instance != null)
            GameHUD.Instance.RefreshFromPlayerState(this);
    }

    public void PlayLocalDamageFeedback()
    {
        if (hitVfx != null) hitVfx.SetActive(true);
    }
}