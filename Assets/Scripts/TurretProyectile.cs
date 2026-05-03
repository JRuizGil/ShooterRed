using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

// Proyectil de torreta — necesita NetworkBehaviour para funcionar en red

public class TurretProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 10f;

    [Networked] private TickTimer LifeTimer { get; set; }

    private Vector3 direction;

    public void Initialize(Vector3 fireDirection, TurretSystem turret)
    {
        direction = fireDirection;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        transform.position += direction * speed * Runner.DeltaTime;

        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        if (Physics.Raycast(
            transform.position - direction * speed * Runner.DeltaTime,
            direction,
            out RaycastHit hit,
            speed * Runner.DeltaTime * 2f))
        {
            if (!hit.collider.CompareTag("Turret"))
            {
                PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
                if (targetHealth != null)
                    targetHealth.TakeDamage(hit.collider.gameObject.name, PlayerRef.None);

                Runner.Despawn(Object);
            }
        }
    }
}
