using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHits      = 4;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Feedback")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private Material   damagedMaterial;

    [Networked] public int          HitsRemaining    { get; set; }
    [Networked] public NetworkBool  IsAlive          { get; set; }
    [Networked] public float        RespawnTimer     { get; set; }

    private PlayerRef ownerPlayer;

    // =========================================================
    public override void Spawned()
    {
        ownerPlayer = Object.InputAuthority;

        if (Object.HasStateAuthority)
        {
            HitsRemaining = maxHits;
            IsAlive       = true;
            RespawnTimer  = 0f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || IsAlive) return;

        RespawnTimer -= Runner.DeltaTime;
        if (RespawnTimer <= 0f)
            Respawn();
    }

    // =========================================================
    // DAÑO
    // =========================================================
    public void TakeDamage(string partName, PlayerRef attacker)
    {
        if (!Object.HasStateAuthority || !IsAlive) return;

        SpawnProtection sp = GetComponent<SpawnProtection>();
        if (sp != null && !sp.CanTakeDamage()) return;

        HitsRemaining--;
        RPC_HitFeedback(transform.position);

        if (HitsRemaining <= 0)
            Die(attacker, partName);
    }

    public void TakeDamageHits(int hits, PlayerRef attacker)
    {
        if (!Object.HasStateAuthority || !IsAlive) return;
        for (int i = 0; i < hits && HitsRemaining > 0; i++)
            TakeDamage("hit", attacker);
    }

    public void TakeDamage(int amount, PlayerRef attacker, string cause)
    {
        if (!Object.HasStateAuthority || !IsAlive) return;
        HitsRemaining = Mathf.Max(0, HitsRemaining - amount);
        RPC_HitFeedback(transform.position);
        if (HitsRemaining <= 0)
            Die(attacker, cause);
    }

    // =========================================================
    // MUERTE Y RESPAWN
    // =========================================================
    private void Die(PlayerRef killer, string cause)
    {
        IsAlive      = false;
        RespawnTimer = respawnDelay;

        GameState.Instance?.AddKill(killer, ownerPlayer, cause);
        GetComponent<PlayerHabilities>()?.ResetAbilities();

        RPC_OnDie(respawnDelay);
    }

    private void Respawn()
    {
        HitsRemaining = maxHits;
        IsAlive       = true;
        RespawnTimer  = 0f;
        RPC_OnRespawn();
    }

    // =========================================================
    // RPCs — feedback visual en todos los clientes
    // =========================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HitFeedback(Vector3 pos)
    {
        if (hitVfxPrefab != null)
            Instantiate(hitVfxPrefab, pos, Quaternion.identity);

        // Feedback en crosshair del atacante — via HUD local
        if (Object.HasInputAuthority)
            GetComponent<PlayerState>()?.PlayLocalDamageFeedback();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDie(float delay)
    {
        // Solo el jugador local ve su pantalla de muerte
        if (Object.HasInputAuthority)
            GameHUD.Instance?.ShowDeathScreen(delay);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnRespawn()
    {
        if (Object.HasInputAuthority)
            GameHUD.Instance?.HideDeathScreen();
    }

    // =========================================================
    // API PÚBLICA
    // =========================================================
    public bool  GetIsAlive()      => IsAlive;
    public int   GetHitsRemaining() => HitsRemaining;
    public void  PublicRespawn()   => Respawn();
}