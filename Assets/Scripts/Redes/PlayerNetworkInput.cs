using Fusion;

// Enum de botones del jugador
public enum PlayerButtons
{
    Fire = 0,
    Jump = 1,
    NextWeapon = 2,  // añadir
    PrevWeapon = 3   // añadir
    
};
public struct PlayerNetworkInput : INetworkInput
{
    public NetworkButtons Buttons;
    public UnityEngine.Vector2 MoveDirection;
    
    
}