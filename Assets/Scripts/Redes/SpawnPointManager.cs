using UnityEngine;
using System.Collections.Generic;

public class SpawnPointManager : MonoBehaviour
{
    [SerializeField]
    private List<Transform> spawnPoints = new List<Transform>();

    private int currentSpawnIndex = 0;

    private void OnDrawGizmos()
    {
        // Dibuja gizmos para visualizar los spawn points en el editor
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] == null) continue;

            // Dibujar esfera en cada spawn point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(spawnPoints[i].position, 0.5f);

            // Dibujar flecha indicando rotación
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(spawnPoints[i].position, spawnPoints[i].forward * 1.5f);

            // Label
            Vector3 labelPos = spawnPoints[i].position + Vector3.up * 1f;
            Debug.DrawLine(spawnPoints[i].position, labelPos, Color.yellow);
        }
    }

    /// <summary>
    /// Obtener el siguiente punto de spawn (round-robin)
    /// </summary>
    public (Vector3 position, Quaternion rotation) GetNextSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[SpawnPointManager] No spawn points configured!");
            return (Vector3.zero, Quaternion.identity);
        }

        Transform spawnPoint = spawnPoints[currentSpawnIndex];
        currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Count;

        return (spawnPoint.position, spawnPoint.rotation);
    }

    /// <summary>
    /// Obtener un spawn point específico por índice
    /// </summary>
    public (Vector3 position, Quaternion rotation) GetSpawnPointByIndex(int index)
    {
        if (index < 0 || index >= spawnPoints.Count)
        {
            Debug.LogWarning($"[SpawnPointManager] Invalid spawn point index: {index}");
            return (Vector3.zero, Quaternion.identity);
        }

        Transform spawnPoint = spawnPoints[index];
        return (spawnPoint.position, spawnPoint.rotation);
    }

    /// <summary>
    /// Obtener la cantidad de spawn points configurados
    /// </summary>
    public int GetSpawnPointCount()
    {
        return spawnPoints.Count;
    }

    /// <summary>
    /// Reiniciar el índice de spawn (útil para nueva partida)
    /// </summary>
    public void ResetSpawnIndex()
    {
        currentSpawnIndex = 0;
    }

    /// <summary>
    /// Agregar un spawn point (para uso dinámico si es necesario)
    /// </summary>
    public void AddSpawnPoint(Transform point)
    {
        if (point != null && !spawnPoints.Contains(point))
        {
            spawnPoints.Add(point);
        }
    }
}
