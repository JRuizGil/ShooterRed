using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;

public class PlayerSceneInitializer : MonoBehaviour
{
    private static bool initialized = false;

    private void Awake()
    {
        // Only run if this is PlayerScene
        if (SceneManager.GetActiveScene().name != "PlayerScene")
            return;

        // Prevent multiple initializations
        if (initialized)
            return;

        initialized = true;

        // Check if SpawnPointsManager already exists
        SpawnPointManager existingSpawner = FindObjectOfType<SpawnPointManager>();
        if (existingSpawner != null && existingSpawner.GetSpawnPointCount() > 0)
        {
            Debug.Log("[PlayerSceneInitializer] SpawnPointManager already initialized");
            return;
        }

        // Create SpawnPointsManager GameObject if it doesn't exist
        if (existingSpawner == null)
        {
            GameObject spawnManagerObj = new GameObject("SpawnPointsManager");
            existingSpawner = spawnManagerObj.AddComponent<SpawnPointManager>();
            Debug.Log("[PlayerSceneInitializer] Created new SpawnPointsManager");
        }

        // Create 4 spawn points in different locations
        Vector3[] spawnPositions = new Vector3[]
        {
            new Vector3(-5, 1, -5),   // Front-Left
            new Vector3(5, 1, -5),    // Front-Right
            new Vector3(-5, 1, 5),    // Back-Left
            new Vector3(5, 1, 5)      // Back-Right
        };

        List<Transform> spawnPointTransforms = new List<Transform>();

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            GameObject spawnPointObj = new GameObject($"SpawnPoint_{i + 1}");
            spawnPointObj.transform.position = spawnPositions[i];
            spawnPointObj.transform.rotation = Quaternion.identity;
            spawnPointTransforms.Add(spawnPointObj.transform);

            Debug.Log($"[PlayerSceneInitializer] Created {spawnPointObj.name} at {spawnPositions[i]}");
        }

        // Assign spawn points to SpawnPointManager via reflection
        FieldInfo field = typeof(SpawnPointManager).GetField("spawnPoints", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(existingSpawner, spawnPointTransforms);
            Debug.Log($"[PlayerSceneInitializer] Assigned {spawnPointTransforms.Count} spawn points to SpawnPointManager");
        }
        else
        {
            Debug.LogError("[PlayerSceneInitializer] Could not find spawnPoints field in SpawnPointManager");
        }
    }
}
