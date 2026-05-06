using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Networked] public PlayerRef OwnerPlayer { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

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
        PlayerRegistry.Unregister(OwnerPlayer);
        Debug.Log($"[PlayerState] Despawned: {OwnerPlayer}");
    }

    private void OnHealthChanged()   => RefreshHealthVisuals();
    private void OnPlayerNameChanged() => Debug.Log($"[PlayerState] Nombre: {PlayerName}");

    private void RefreshHealthVisuals()
    {
        if (Object.HasInputAuthority && GameHUD.Instance != null)
            GameHUD.Instance.RefreshFromPlayerState(this);
    }

    // Feedback visual local — llamado desde PlayerHealth.RPC_HitFeedback
    public void PlayLocalDamageFeedback()
    {
        GameHUD.Instance?.ShowHitFeedback();
    }
}