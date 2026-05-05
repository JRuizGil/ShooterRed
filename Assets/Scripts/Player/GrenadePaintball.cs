using Fusion;
using UnityEngine;

/// <summary>
/// Granada de pintura que explota al impactar
/// Causa daño en área dentro de un radio
/// </summary>
public class GrenadePaintball : MonoBehaviour
{
    private int damage;
    private float explosionRadius;
    private PlayerRef thrower;
    private bool hasExploded = false;
    private ParticleSystem explosionParticles;

    [Header("Explosion Settings")]
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionDelay = 0.1f;

    public void Initialize(int damageAmount, float radius, PlayerRef throwerRef)
    {
        damage = damageAmount;
        explosionRadius = radius;
        thrower = throwerRef;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded)
            return;

        // Explotar al impactar
        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        // Crear efecto visual de explosión
        CreateExplosionEffect();

        // Reproducir sonido
        AudioManager.PlayGrenadeExplosion(transform.position);

        // Detectar enemigos en el radio
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider collider in hitColliders)
        {
            PlayerHealth playerHealth = collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // No dañar al que lanzó la granada
                if (playerHealth.GetComponent<PlayerState>().OwnerPlayer != thrower)
                {
                    playerHealth.TakeDamage(damage, thrower, "Grenade");
                }
            }
        }

        Debug.Log($"[GrenadePaintball] Exploded at {transform.position}, radius: {explosionRadius}");
        Destroy(gameObject);
    }

    private void CreateExplosionEffect()
    {
        // Crear esfera visual de explosión
        GameObject effectSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effectSphere.transform.position = transform.position;
        effectSphere.transform.localScale = Vector3.one * (explosionRadius * 2);

        Collider col = effectSphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.6f, 0f, 0.6f);
        effectSphere.GetComponent<Renderer>().material = mat;

        Destroy(effectSphere, 0.5f);
    }
}
