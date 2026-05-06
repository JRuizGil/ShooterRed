using Fusion;
using UnityEngine;
using System.Collections;

public class SpawnProtection : NetworkBehaviour
{
    [Header("Spawn Protection")]
    [SerializeField] private float protectionDuration = 2f;
    [SerializeField] private Material protectionMaterial;
    [SerializeField] private float blinkInterval = 0.2f;

    // ✅ Networked para que todos los clientes vean el parpadeo
    [Networked, OnChangedRender(nameof(OnProtectionChanged))]
    public NetworkBool IsProtected { get; set; }

    private PlayerHealth playerHealth;
    private Renderer[]   renderers;
    private Material[]   originalMaterials;
    private Coroutine    blinkCoroutine;

    public override void Spawned()
    {
        playerHealth = GetComponent<PlayerHealth>();
        renderers    = GetComponentsInChildren<Renderer>();

        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].material;
    }

    // Llamado por RespawnManager tras el respawn
    public void ActivateSpawnProtection()
    {
        if (!Object.HasStateAuthority) return;
        IsProtected = true;

        // Desactivar protección después del tiempo
        StartCoroutine(DeactivateAfterDelay());
    }

    private IEnumerator DeactivateAfterDelay()
    {
        yield return new WaitForSeconds(protectionDuration);
        if (Object.HasStateAuthority)
            IsProtected = false;
    }

    // ✅ OnChangedRender se ejecuta en TODOS los clientes cuando IsProtected cambia
    private void OnProtectionChanged()
    {
        if (IsProtected)
            StartBlink();
        else
            StopBlink();
    }

    private void StartBlink()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        // Restaurar materiales
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled  = true;
                renderers[i].material = originalMaterials[i];
            }
        }
    }

    private IEnumerator BlinkRoutine()
    {
        // Aplicar material de protección
        if (protectionMaterial != null)
            foreach (var r in renderers)
                if (r != null) r.material = protectionMaterial;

        float elapsed = 0f;
        while (IsProtected)
        {
            bool visible = Mathf.FloorToInt(elapsed / blinkInterval) % 2 == 0;
            foreach (var r in renderers)
                if (r != null) r.enabled = visible;

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopBlink();
    }

    // Para PlayerHealth: bloquear daño si está protegido
    public bool CanTakeDamage() => !IsProtected;
}