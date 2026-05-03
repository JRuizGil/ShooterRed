using Fusion;
using UnityEngine;

public class NetworkBullet : NetworkBehaviour
{
    [Networked] private TickTimer LifeTimer { get; set; }

    private Rigidbody rb;
    private const float LifeTime = 5f;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();

        if (Object.HasStateAuthority)
            LifeTimer = TickTimer.CreateFromSeconds(Runner, LifeTime);
    }

    public void Init(Vector3 velocity)
    {
        if (rb != null)
            rb.linearVelocity = velocity;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (LifeTimer.Expired(Runner))
            Runner.Despawn(Object);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.CompareTag("PlayerCollider"))
        {
            PlayerHealth targetHealth = other.GetComponentInParent<PlayerHealth>();
        }
        if (other.CompareTag("HeadCollider"))
        {
            PlayerHealth targetHealth = other.GetComponentInParent<PlayerHealth>();
            targetHealth.TakeDamage("Head", Object.InputAuthority);
        }
        Runner.Despawn(Object);
    }
}