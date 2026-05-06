using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawnea armas como NetworkObjects.
/// Fusion replica automáticamente el estado a todos los clientes,
/// incluyendo los que entran tarde.
/// Requiere NetworkObject en este GameObject.
/// </summary>
public class WeaponSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private int      weaponCount  = 10;
    [SerializeField] private float    spawnRadius  = 500f;
    [SerializeField] private float    spawnHeight  = 1f;
    [SerializeField] private Vector3  mapCenter    = Vector3.zero;

    [Header("Weapon Prefabs — deben tener NetworkObject")]
    [SerializeField] private List<NetworkPrefabRef> weaponPrefabs = new List<NetworkPrefabRef>();

    [Header("Raycast")]
    [SerializeField] private float     raycastHeight = 100f;
    [SerializeField] private LayerMask groundLayer   = ~0;

    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return;

        // ✅ Solo spawnear armas en la escena de juego, no en el lobby
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != 2)
        {
            Debug.Log("[WeaponSpawner] No es la escena de juego, omitiendo spawn de armas");
            return;
        }

        if (weaponPrefabs == null || weaponPrefabs.Count == 0)
        {
            Debug.LogError("[WeaponSpawner] No hay prefabs asignados!");
            return;
        }

        SpawnWeapons();
    }

    private void SpawnWeapons()
    {
        int spawned  = 0;
        int attempts = 0;

        while (spawned < weaponCount && attempts < weaponCount * 5)
        {
            attempts++;

            NetworkPrefabRef prefab = weaponPrefabs[Random.Range(0, weaponPrefabs.Count)];
            if (!prefab.IsValid) continue;

            Vector3 pos  = GetRandomPosition();
            float   rotY = Random.Range(0f, 360f);

            // Spawn networked — Fusion se encarga de replicar a todos
            NetworkObject obj = Runner.Spawn(
                prefab,
                pos,
                Quaternion.Euler(0, rotY, 0)
                // Sin inputAuthority — nadie es dueño, es un objeto del mundo
            );

            if (obj != null)
            {
                spawned++;
                Debug.Log($"[WeaponSpawner] Arma {spawned}/{weaponCount} en {pos}");
            }
        }

        Debug.Log($"[WeaponSpawner] {spawned} armas spawneadas");
    }

    private Vector3 GetRandomPosition()
    {
        Vector2 circle    = Random.insideUnitCircle * spawnRadius;
        Vector3 candidate = mapCenter + new Vector3(circle.x, raycastHeight, circle.y);

        if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
            return hit.point + Vector3.up * spawnHeight;

        return mapCenter + new Vector3(circle.x, spawnHeight, circle.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawSphere(mapCenter, spawnRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(mapCenter, spawnRadius);
    }
}