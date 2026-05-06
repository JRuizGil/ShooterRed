using Fusion;
using UnityEngine;

public class TurretSystem : NetworkBehaviour
{
    [Header("Turret Settings")]
    [SerializeField] private float detectionRange = 30f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("References")]
    [SerializeField] private Transform barrelTransform;
    [SerializeField] private NetworkPrefabRef projectilePrefab; // NetworkPrefabRef, no GameObject
    [SerializeField] private Transform firePoint;

    [Header("Effects")]
    [SerializeField] private GameObject muzzleFlashPrefab;

    [Networked] public NetworkId CurrentTargetId { get; set; } // NetworkId en vez de NetworkObject
    [Networked] public float NextFireTime { get; set; }

    private NetworkObject currentTargetObject; // referencia local resuelta
    private Collider[] detectionResults = new Collider[10];

    public override void Spawned()
    {
        if (firePoint == null) firePoint = transform;
        if (barrelTransform == null) barrelTransform = transform;

        if (Object.HasStateAuthority)
            NextFireTime = 0f;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        FindNearestTarget();

        // Resolver NetworkId → NetworkObject localmente
        currentTargetObject = CurrentTargetId.IsValid
            ? Runner.FindObject(CurrentTargetId)
            : null;

        if (currentTargetObject != null)
        {
            AimAtTarget();
            CheckFire();
        }
        else
        {
            RotateTowards(barrelTransform.forward + Vector3.up * 0.5f);
        }
    }

    private void FindNearestTarget()
    {
        int hits = Physics.OverlapSphereNonAlloc(
            transform.position, detectionRange, detectionResults, enemyLayer);

        NetworkObject closestTarget = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits; i++)
        {
            if (detectionResults[i] == null) continue;

            PlayerHealth playerHealth = detectionResults[i].GetComponent<PlayerHealth>();
            if (playerHealth == null || !playerHealth.GetIsAlive()) continue;

            NetworkObject netObj = detectionResults[i].GetComponent<NetworkObject>();
            if (netObj == null) continue;

            float distance = Vector3.Distance(transform.position, detectionResults[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = netObj;
            }
        }

        // Guardar el NetworkId (tipo válido en [Networked])
        CurrentTargetId = closestTarget != null ? closestTarget.Id : default;
    }

    private void AimAtTarget()
    {
        if (currentTargetObject == null) return;
        Vector3 dir = (currentTargetObject.transform.position - barrelTransform.position).normalized;
        RotateTowards(dir);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        barrelTransform.rotation = Quaternion.RotateTowards(
            barrelTransform.rotation,
            targetRotation,
            rotationSpeed * Runner.DeltaTime  // Runner.DeltaTime en FixedUpdateNetwork
        );
    }

    private void CheckFire()
    {
        if (Runner.SimulationTime >= NextFireTime)
        {
            Fire();
            NextFireTime = Runner.SimulationTime + fireRate;
        }
    }

    private void Fire()
    {
        if (currentTargetObject == null) return;

        RPC_PlayTurretFireEffects();

        // Opción A: proyectil de red (si tienes prefab configurado)
        if (projectilePrefab.IsValid)
        {
            Vector3 dir = (currentTargetObject.transform.position - firePoint.position).normalized;
            var projObj = Runner.Spawn(
                projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(dir),
                Object.StateAuthority
            );
            projObj.GetComponent<TurretProjectile>()?.Initialize(dir, this);
        }
        else
        {
            // Opción B: raycast directo (más simple y barato en red)
            if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, detectionRange))
            {
                PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
                if (targetHealth != null)
                    targetHealth.TakeDamage(hit.collider.gameObject.name, PlayerRef.None);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayTurretFireEffects()
    {
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.15f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}

