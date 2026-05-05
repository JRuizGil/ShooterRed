using UnityEngine;

/// <summary>
/// Sistema de efectos visuales de impacto
/// Maneja explosiones, salpicaduras de pintura y efectos de daño
/// </summary>
public class ImpactEffects : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject bulletImpactPrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject bloodSplashPrefab;

    [Header("Particle Settings")]
    [SerializeField] private float particleLifetime = 2f;
    [SerializeField] private float explosionScale = 2f;

    private static ImpactEffects instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Crear efecto de impacto de bala
    /// </summary>
    public static void PlayBulletImpact(Vector3 position, Vector3 normal)
    {
        if (instance == null)
            return;

        // Crear esfera de impacto visual
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.transform.position = position;
        impact.transform.localScale = Vector3.one * 0.3f;

        // Remover collider
        Collider col = impact.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Aplicar material color
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.8f, 0f, 0.8f);
        impact.GetComponent<Renderer>().material = mat;

        // Destruir después de cierto tiempo
        Destroy(impact, instance.particleLifetime);

        // Raycast línea de impacto (visual)
        Debug.DrawLine(position, position + normal * 2f, Color.yellow, instance.particleLifetime);
    }

    /// <summary>
    /// Crear efecto de explosión
    /// </summary>
    public static void PlayExplosion(Vector3 position, float radius = 5f)
    {
        if (instance == null)
            return;

        // Crear esfera de explosión
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.transform.position = position;
        explosion.transform.localScale = Vector3.one * (radius * 0.4f);
        explosion.name = "Explosion";

        Collider col = explosion.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.5f, 0f, 0.7f);
        explosion.GetComponent<Renderer>().material = mat;

        Destroy(explosion, instance.particleLifetime);

        // Efecto de onda expansiva
        instance.StartCoroutine(ScaleExplosion(explosion.transform, instance.particleLifetime));

        // Debug: dibujar radio de explosión
        DebugDrawCircle(position, radius, Color.red, instance.particleLifetime);
    }

    /// <summary>
    /// Crear salpicadura de pintura/sangre
    /// </summary>
    public static void PlayBloodSplash(Vector3 position, Vector3 normal)
    {
        if (instance == null)
            return;

        // Crear efecto de salpicadura
        GameObject splash = GameObject.CreatePrimitive(PrimitiveType.Quad);
        splash.transform.position = position;
        splash.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        splash.transform.localScale = Vector3.one * 0.5f;

        Collider col = splash.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0f, 0f, 0.7f);
        splash.GetComponent<Renderer>().material = mat;

        Destroy(splash, instance.particleLifetime);
    }

    /// <summary>
    /// Crear destello de impacto
    /// </summary>
    public static void PlayImpactFlash(Vector3 position)
    {
        if (instance == null)
            return;

        GameObject flash = new GameObject("ImpactFlash");
        flash.transform.position = position;

        Light light = flash.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 10f;
        light.intensity = 2f;
        light.color = Color.yellow;

        // Fade out gradual
        Material mat = new Material(Shader.Find("Standard"));
        Destroy(flash, 0.2f);
    }

    // Corrutina para animar expansión de explosión
    public static System.Collections.IEnumerator ScaleExplosion(Transform explosionTransform, float duration)
    {
        Vector3 startScale = explosionTransform.localScale;
        float startTime = Time.time;
        float endTime = startTime + duration;

        while (Time.time < endTime && explosionTransform != null)
        {
            float progress = (Time.time - startTime) / duration;
            explosionTransform.localScale = startScale * (1f + progress * 0.5f);
            yield return null;
        }
    }

    // Debug: dibujar círculo en el mundo
    private static void DebugDrawCircle(Vector3 center, float radius, Color color, float duration)
    {
        int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * Mathf.PI * 2f;
            float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            Debug.DrawLine(p1, p2, color, duration);
        }
    }

    public static ImpactEffects Instance => instance;
}
