using Fusion;
using UnityEngine;

public class NetworkBullet : NetworkBehaviour
{
    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private Vector3   Velocity  { get; set; }

    // ✅ Posición networked — el host la mueve, los clientes la leen
    [Networked] private Vector3 NetworkedPos { get; set; }

    private const float LifeTime = 5f;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            LifeTimer   = TickTimer.CreateFromSeconds(Runner, LifeTime);
            NetworkedPos = transform.position;
        }

        // Desactivar Rigidbody si existe — no lo necesitamos
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        // Desactivar collider en clientes — solo el host detecta colisiones
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = Object.HasStateAuthority;
    }

    public void Init(Vector3 velocity)
    {
        Velocity     = velocity;
        NetworkedPos = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        // Solo el host mueve la bala
        if (!Object.HasStateAuthority) return;

        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // Mover manualmente — sin Rigidbody
        transform.position += Velocity * Runner.DeltaTime;
        NetworkedPos        = transform.position;
    }

    public override void Render()
    {
        // ✅ Todos los clientes aplican la posición networked del host
        // Sin física local — posición siempre correcta y desde el punto de vista correcto
        if (!Object.HasStateAuthority)
            transform.position = NetworkedPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo el host procesa colisiones
        if (!Object.HasStateAuthority) return;

        Vector3 impactPos    = other.bounds.center;
        Vector3 impactNormal = (impactPos - transform.position).normalized;

        if (other.CompareTag("PlayerCollider"))
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage("Torso", Object.InputAuthority);
                ImpactEffects.PlayBloodSplash(impactPos, impactNormal);
            }
        }
        else if (other.CompareTag("HeadCollider"))
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage("Head", Object.InputAuthority);
                ImpactEffects.PlayBloodSplash(impactPos, impactNormal);
                ImpactEffects.PlayImpactFlash(impactPos);
            }
        }
        else
        {
            ImpactEffects.PlayBulletImpact(impactPos, impactNormal);
        }

        Runner.Despawn(Object);
    }
}