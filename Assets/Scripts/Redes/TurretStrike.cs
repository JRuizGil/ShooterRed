using Fusion;
using UnityEngine;

public class TurretStrike : MonoBehaviour
{
    private PlayerRef owner;
    private float duration;
    private float spawnTime;
    private bool isActive = true;

    [Header("Turret Settings")]
    [SerializeField] private float detectionRadius = 25f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private Transform gunBarrel;

    private float lastFireTime = 0f;
    private Transform currentTarget;
    private PlayerHealth currentTargetHealth;

    [Header("Visual Components")]
    [SerializeField] private Transform turretHead;
    [SerializeField] private LineRenderer shootLine;

    public void Initialize(PlayerRef ownerRef, float durationSeconds)
    {
        owner = ownerRef;
        duration = durationSeconds;
        spawnTime = Time.time;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        if (gunBarrel == null) gunBarrel = transform;
        if (turretHead == null) turretHead = transform.Find("Head") ?? transform;
    }

    private void Update()
    {
        if (!isActive) return;

        if (Time.time - spawnTime > duration)
        {
            Deactivate();
            return;
        }

        FindTarget();

        if (currentTarget != null && Time.time - lastFireTime > fireRate)
            AimAndShoot();
    }

    private void FindTarget()
    {
        PlayerState[] allPlayers = FindObjectsByType<PlayerState>(FindObjectsSortMode.None);
        float closestDistance = detectionRadius;
        Transform closestTarget = null;
        PlayerHealth closestHealth = null;

        foreach (PlayerState player in allPlayers)
        {
            if (player.OwnerPlayer == owner) continue;

            PlayerHealth health = player.GetComponent<PlayerHealth>();

            // CORREGIDO: IsDead → !GetIsAlive()
            if (health == null || !health.GetIsAlive()) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = player.transform;
                closestHealth = health;
            }
        }

        currentTarget = closestTarget;
        currentTargetHealth = closestHealth;
    }

    private void AimAndShoot()
    {
        if (currentTarget == null) return;

        Vector3 directionToTarget = (currentTarget.position - turretHead.position).normalized;
        turretHead.rotation = Quaternion.LookRotation(directionToTarget);

        Vector3 shootDirection = (currentTarget.position - gunBarrel.position).normalized;

        if (Physics.Raycast(gunBarrel.position, shootDirection, out RaycastHit hit, detectionRadius))
        {
            PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
            if (targetHealth != null && targetHealth.GetIsAlive())
            {
                // CORREGIDO: TakeDamage(string partName, PlayerRef attacker)
                // Usamos TakeDamageHits para daño numérico desde habilidades
                targetHealth.TakeDamageHits(1, owner);
            }
        }

        if (shootLine != null)
        {
            shootLine.SetPosition(0, gunBarrel.position);
            shootLine.SetPosition(1, gunBarrel.position + shootDirection * 50f);
        }

        lastFireTime = Time.time;
    }

    private void Deactivate()
    {
        isActive = false;
        Destroy(gameObject);
    }
}