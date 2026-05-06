using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawnea armas aleatoriamente en el mapa al inicio de la partida.
/// Solo el host ejecuta el spawn — Fusion las replica a todos los clientes.
/// Coloca este componente en un GameObject vacío en la PlayerScene.
/// </summary>
public class WeaponSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private int weaponCount    = 10;
    [SerializeField] private float spawnRadius  = 500f;
    [SerializeField] private float spawnHeight  = 1f;   // altura sobre el suelo
    [SerializeField] private Vector3 mapCenter  = Vector3.zero;

    [Header("Weapon Prefabs")]
    [SerializeField] private List<NetworkPrefabRef> weaponPrefabs = new List<NetworkPrefabRef>();

    [Header("Raycast Settings")]
    [SerializeField] private float raycastHeight   = 100f;  // desde donde baja el rayo
    [SerializeField] private LayerMask groundLayer = ~0;    // todo por defecto

    public override void Spawned()
    {
        // Solo el host spawnea armas
        if (!Object.HasStateAuthority) return;

        if (weaponPrefabs == null || weaponPrefabs.Count == 0)
        {
            Debug.LogError("[WeaponSpawner] No hay prefabs de armas asignados en el Inspector!");
            return;
        }

        SpawnWeapons();
    }

    private void SpawnWeapons()
    {
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = weaponCount * 5; // evitar loop infinito

        while (spawned < weaponCount && attempts < maxAttempts)
        {
            attempts++;

            Vector3 spawnPos = GetRandomPosition();
            if (spawnPos == Vector3.zero) continue;

            // Elegir prefab aleatorio
            NetworkPrefabRef prefab = weaponPrefabs[Random.Range(0, weaponPrefabs.Count)];
            if (!prefab.IsValid)
            {
                Debug.LogWarning("[WeaponSpawner] Prefab inválido, omitiendo");
                continue;
            }

            NetworkObject obj = Runner.Spawn(prefab, spawnPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));

            if (obj != null)
            {
                spawned++;
                Debug.Log($"[WeaponSpawner] Arma {spawned}/{weaponCount} spawneada en {spawnPos}");
            }
        }

        Debug.Log($"[WeaponSpawner] Spawn completado: {spawned} armas en {attempts} intentos");
    }

    // Genera una posición aleatoria dentro del radio y ajusta a la altura del suelo
    private Vector3 GetRandomPosition()
    {
        // Punto aleatorio en círculo
        Vector2 circle   = Random.insideUnitCircle * spawnRadius;
        Vector3 candidate = mapCenter + new Vector3(circle.x, raycastHeight, circle.y);

        // Raycast hacia abajo para encontrar el suelo
        if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
        {
            return hit.point + Vector3.up * spawnHeight;
        }

        // Si no hay suelo bajo ese punto, usar altura fija
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