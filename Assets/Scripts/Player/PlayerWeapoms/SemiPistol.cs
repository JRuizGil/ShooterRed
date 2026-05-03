using Fusion;
using UnityEngine;

public class SemiPistol : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 20f;

    // Guardamos el estado del botón en el tick anterior para detectar WasPressed
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    public override void FixedUpdateNetwork()
    {
        // Solo el cliente con InputAuthority lee su propio input
        if (!Object.HasInputAuthority) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Fire);
            PreviousButtons = input.Buttons;

            if (firePressed)
                RPC_RequestFire();
        }
    }

    // El cliente pide disparar → Fusion enruta al servidor
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestFire()
    {
        var bullet = Runner.Spawn(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation,
            Object.InputAuthority
        );

        bullet.GetComponent<NetworkBullet>().Init(firePoint.forward * bulletSpeed);
    }
}