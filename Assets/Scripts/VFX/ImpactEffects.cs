using Fusion;
using UnityEngine;

/// <summary>
/// Singleton de efectos visuales de impacto.
/// Coloca este componente en un GameObject en la PlayerScene.
/// Los efectos se disparan via RPC desde NetworkBullet para que
/// todos los clientes los vean.
/// </summary>
public class ImpactEffects : NetworkBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private float particleLifetime = 2f;

    private static ImpactEffects instance;

    public static ImpactEffects Instance => instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    // =========================================================
    // API ESTÁTICA — llamar desde cualquier script
    // El host llama estos métodos → RPC a todos los clientes
    // =========================================================

    public static void PlayBulletImpact(Vector3 position, Vector3 normal)
    {
        if (instance == null) { Debug.LogWarning("[ImpactEffects] Instance null!"); return; }
        instance.RPC_BulletImpact(position, normal);
    }

    public static void PlayBloodSplash(Vector3 position, Vector3 normal)
    {
        if (instance == null) return;
        instance.RPC_BloodSplash(position, normal);
    }

    public static void PlayImpactFlash(Vector3 position)
    {
        if (instance == null) return;
        instance.RPC_ImpactFlash(position);
    }

    public static void PlayExplosion(Vector3 position, float radius = 5f)
    {
        if (instance == null) return;
        instance.RPC_Explosion(position, radius);
    }

    // =========================================================
    // RPCs — host los llama, todos los clientes los ejecutan
    // =========================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BulletImpact(Vector3 position, Vector3 normal)
    {
        SpawnBulletImpact(position, normal);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BloodSplash(Vector3 position, Vector3 normal)
    {
        SpawnBloodSplash(position, normal);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ImpactFlash(Vector3 position)
    {
        SpawnImpactFlash(position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Explosion(Vector3 position, float radius)
    {
        SpawnExplosion(position, radius);
    }

    // =========================================================
    // IMPLEMENTACIÓN LOCAL — se ejecuta en cada cliente
    // =========================================================

    private void SpawnBulletImpact(Vector3 position, Vector3 normal)
    {
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.transform.position   = position;
        impact.transform.localScale = Vector3.one * 0.3f;
        Destroy(impact.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0.8f, 0f, 0.8f);
        impact.GetComponent<Renderer>().material = mat;
        Destroy(impact, particleLifetime);

        Debug.DrawLine(position, position + normal * 2f, Color.yellow, particleLifetime);
    }

    private void SpawnBloodSplash(Vector3 position, Vector3 normal)
    {
        GameObject splash = GameObject.CreatePrimitive(PrimitiveType.Quad);
        splash.transform.position   = position;
        splash.transform.rotation   = Quaternion.FromToRotation(Vector3.up, normal);
        splash.transform.localScale = Vector3.one * 0.5f;
        Destroy(splash.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0f, 0f, 0.7f);
        splash.GetComponent<Renderer>().material = mat;
        Destroy(splash, particleLifetime);
    }

    private void SpawnImpactFlash(Vector3 position)
    {
        GameObject flash = new GameObject("ImpactFlash");
        flash.transform.position = position;

        Light light       = flash.AddComponent<Light>();
        light.type        = LightType.Point;
        light.range       = 10f;
        light.intensity   = 2f;
        light.color       = Color.yellow;

        Destroy(flash, 0.2f);
    }

    private void SpawnExplosion(Vector3 position, float radius)
    {
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.transform.position   = position;
        explosion.transform.localScale = Vector3.one * (radius * 0.4f);
        Destroy(explosion.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0.5f, 0f, 0.7f);
        explosion.GetComponent<Renderer>().material = mat;

        StartCoroutine(ScaleAndDestroy(explosion.transform, particleLifetime));
    }

    private System.Collections.IEnumerator ScaleAndDestroy(Transform t, float duration)
    {
        Vector3 startScale = t.localScale;
        float elapsed = 0f;
        while (elapsed < duration && t != null)
        {
            elapsed += Time.deltaTime;
            t.localScale = startScale * (1f + (elapsed / duration) * 0.5f);
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }
}