using Fusion;

public enum PlayerButtons
{
    Fire       = 0,
    Jump       = 1,
    NextWeapon = 2,
    PrevWeapon = 3
}

public struct PlayerNetworkInput : INetworkInput
{
    public NetworkButtons Buttons;
    public UnityEngine.Vector2 MoveDirection;

    // ✅ Rotación Y del jugador en el momento de mandar el input
    // El host usa este valor para calcular la dirección de movimiento correcta
    // evitando que use la rotación interpolada (que llega con retraso)
    public float YawAngle;
}