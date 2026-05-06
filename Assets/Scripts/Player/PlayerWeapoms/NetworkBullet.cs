using Fusion;
using UnityEngine;

// Bala de arma que se instancia en todos los clientes y se desplaza fielmente
public class NetworkBullet : NetworkBehaviour
{
    [Networked] private Vector3 NetworkedVelocity { get; set; }
    [Networked] private TickTimer LifeTimer { get; set; }

    private Rigidbody rb;
    private const float BulletLifetime = 10f;
    private bool hasCollided = false;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        hasCollided = false;

        // El estado authority inicia el temporizador de vida
        if (Object.HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, BulletLifetime);
        }

        // Todos los clientes aplican la velocidad networked al spawnearse
        if (rb != null && NetworkedVelocity != Vector3.zero)
        {
            rb.linearVelocity = NetworkedVelocity;
        }
    }

    // Se llama ANTES de Spawned() para inicializar la velocidad
    public void Init(Vector3 velocity)
    {
        NetworkedVelocity = velocity;
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Despawnear si pasó el tiempo de vida
        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    // Sincronizar velocidad en clientes que no tienen autoridad
    public override void Render()
    {
        if (rb == null) return;

        // Si no somos el estado authority y la velocidad está en cero, resincronizar
        if (!Object.HasStateAuthority && rb.linearVelocity.sqrMagnitude < 0.01f && NetworkedVelocity.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = NetworkedVelocity;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo el servidor procesa colisiones
        if (!Object.HasStateAuthority || hasCollided) return;

        hasCollided = true;

        Vector3 impactPos = other.bounds.center;
        Vector3 impactNormal = (impactPos - transform.position).normalized;

        // Colisión con jugador local (no despawnear aún, dejar que continúe)
        if (other.CompareTag("PlayerCollider"))
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                // No aplicar daño al jugador que disparó
                PlayerState playerState = health.GetComponent<PlayerState>();
                if (playerState != null && playerState.OwnerPlayer == Object.InputAuthority)
                {
                    hasCollided = false;
                    return;
                }

                health.TakeDamage("Torso", Object.InputAuthority);
                ImpactEffects.PlayBloodSplash(impactPos, impactNormal);
            }

            // Despawnear después de golpear a otro jugador
            Runner.Despawn(Object);
        }
        else if (other.CompareTag("HeadCollider"))
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                // No aplicar daño al jugador que disparó
                PlayerState playerState = health.GetComponent<PlayerState>();
                if (playerState != null && playerState.OwnerPlayer == Object.InputAuthority)
                {
                    hasCollided = false;
                    return;
                }

                health.TakeDamage("Head", Object.InputAuthority);
                ImpactEffects.PlayBloodSplash(impactPos, impactNormal);
                ImpactEffects.PlayImpactFlash(impactPos);
            }

            // Despawnear después de golpear la cabeza
            Runner.Despawn(Object);
        }
        else if (!other.CompareTag("Player") && !other.CompareTag("PlayerWeapon"))
        {
            // Golpeó una pared u otro objeto - despawnear
            ImpactEffects.PlayBulletImpact(impactPos, impactNormal);
            Runner.Despawn(Object);
        }
    }
}