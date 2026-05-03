using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema de protección de spawn
/// Hace invulnerable al jugador por un tiempo después de respawnear
/// </summary>
public class SpawnProtection : MonoBehaviour
{
    [Header("Spawn Protection")]
    [SerializeField] private float protectionDuration = 2f;
    [SerializeField] private Material protectionMaterial;
    [SerializeField] private float blinkInterval = 0.2f;

    private PlayerHealth playerHealth;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private bool isProtected = false;
    private Coroutine protectionCoroutine;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        renderers = GetComponentsInChildren<Renderer>();

        // Guardar materiales originales
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }
    }

    /// <summary>
    /// Activar protección de spawn
    /// </summary>
    public void ActivateSpawnProtection()
    {
        if (protectionCoroutine != null)
            StopCoroutine(protectionCoroutine);

        protectionCoroutine = StartCoroutine(SpawnProtectionRoutine());
    }

    /// <summary>
    /// Corrutina de protección de spawn
    /// </summary>
    private IEnumerator SpawnProtectionRoutine()
    {
        isProtected = true;
        float elapsedTime = 0f;

        // Cambiar a material de protección (puede ser semitransparente o brillante)
        if (protectionMaterial != null)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.material = protectionMaterial;
            }
        }

        // Parpadear mientras hay protección
        while (elapsedTime < protectionDuration)
        {
            // Mostrar/ocultar cada blinkInterval
            if (Mathf.FloorToInt(elapsedTime / blinkInterval) % 2 == 0)
            {
                SetRenderersActive(true);
            }
            else
            {
                SetRenderersActive(false);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Restaurar materiales originales
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }

        SetRenderersActive(true);
        isProtected = false;

        Debug.Log("[SpawnProtection] Protection ended");
    }

    /// <summary>
    /// Mostrar/ocultar renderizadores
    /// </summary>
    private void SetRenderersActive(bool active)
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = active;
        }
    }

    /// <summary>
    /// Verificar si está protegido
    /// </summary>
    public bool IsProtected()
    {
        return isProtected;
    }

    /// <summary>
    /// Cancelar protección
    /// </summary>
    public void CancelProtection()
    {
        if (protectionCoroutine != null)
        {
            StopCoroutine(protectionCoroutine);
            protectionCoroutine = null;
        }

        // Restaurar materiales
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }

        SetRenderersActive(true);
        isProtected = false;
    }
}
