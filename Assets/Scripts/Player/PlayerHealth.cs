using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Body Parts Setup")]
    [SerializeField] private GameObject[] bodyParts = new GameObject[9];
    [SerializeField] private Material damagedMaterial;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Feedback")]
    [SerializeField] private GameObject hitVfxPrefab;

    [Networked] public int HitsRemaining { get; set; }
    [Networked] public NetworkBool IsAlive { get; set; }
    [Networked] public float RespawnTimer { get; set; }

    private PlayerRef ownerPlayer;
    private HashSet<string> hitPartNames = new HashSet<string>();

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            HitsRemaining = 4;
            IsAlive = true;
            RespawnTimer = 0f;
        }

        InitializeBodyParts();
        hitPartNames.Clear();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (!IsAlive && RespawnTimer > 0f)
        {
            RespawnTimer -= Runner.DeltaTime;
            if (RespawnTimer <= 0f)
                RespawnPlayer();
        }
    }

    // Llamado desde NetworkBullet en el servidor
    public void TakeDamage(string partName, PlayerRef attackerRef)
    {
        // Solo el servidor ejecuta esto
        if (!Object.HasStateAuthority) return;
        if (!IsAlive) return;
        if (hitPartNames.Contains(partName)) return;

        hitPartNames.Add(partName);
        HitsRemaining--;

        // Notificar visualmente a todos los clientes
        RPC_OnPartHit(partName);

        if (HitsRemaining <= 0)
        {
            IsAlive = false;
            RespawnTimer = respawnDelay;

            GameState gameState = FindFirstObjectByType<GameState>();
            if (gameState != null)
                gameState.AddKill(attackerRef, ownerPlayer);
        }
    }

    // Solo visual — se ejecuta en todos los clientes
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnPartHit(string partName)
    {
        foreach (GameObject part in bodyParts)
        {
            if (part != null && part.name == partName)
            {
                Renderer rend = part.GetComponent<Renderer>();
                if (rend != null && damagedMaterial != null)
                    rend.material = damagedMaterial;

                if (hitVfxPrefab != null)
                    Instantiate(hitVfxPrefab, part.transform.position, Quaternion.identity);

                break;
            }
        }
    }

    private void RespawnPlayer()
    {
        IsAlive = true;
        HitsRemaining = 4;
        RespawnTimer = 0f;
        hitPartNames.Clear();

        RPC_OnRespawn();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnRespawn()
    {
        foreach (GameObject part in bodyParts)
        {
            if (part == null) continue;
            Renderer rend = part.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = Color.white;
        }
    }

    private void InitializeBodyParts()
    {
        if (bodyParts == null || bodyParts.Length == 0)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            bodyParts = new GameObject[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                bodyParts[i] = renderers[i].gameObject;
        }
    }

    public bool GetIsAlive() => IsAlive;
    public int GetHitsRemaining() => HitsRemaining;
    public void PublicRespawn() => RespawnPlayer();
}