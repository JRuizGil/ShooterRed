using Fusion;
using UnityEngine;

public class NetworkBullet : NetworkBehaviour
{
    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private Vector3 Velocity { get; set; }

    private Rigidbody rb;
    private const float LifeTime = 5f;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();

        if (Object.HasStateAuthority)
            LifeTimer = TickTimer.CreateFromSeconds(Runner, LifeTime);

        // ✅ Aplicar velocidad networked en todos los clientes al spawnearse
        // rb.linearVelocity se aplica localmente para que la bala se mueva en todos
        if (rb != null)
            rb.linearVelocity = Velocity;
    }

    /// <summary>
    /// Llamado por el host justo antes del spawn (onBeforeSpawned)
    /// para inicializar la velocidad antes de que Spawned() se ejecute
    /// </summary>
    public void Init(Vector3 velocity)
    {
        Velocity = velocity;
        if (rb != null)
            rb.linearVelocity = velocity;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (LifeTimer.Expired(Runner))
            Runner.Despawn(Object);
    }

    // Render: mantener velocidad en clientes remotos (Rigidbody local)
    public override void Render()
    {
        if (!Object.HasInputAuthority && rb != null && rb.linearVelocity == Vector3.zero && Velocity != Vector3.zero)
            rb.linearVelocity = Velocity;
    }

    private void OnTriggerEnter(Collider other)
    {
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