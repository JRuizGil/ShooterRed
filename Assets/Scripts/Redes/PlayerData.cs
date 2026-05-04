using Fusion;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    [Networked] public string PlayerName { get; set; }
    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public int TeamId { get; set; } // 0 = Rojo, 1 = Azul
    
    public override void Spawned()
    {
        Debug.Log($"[PlayerData] Player spawned: {PlayerName} (Owner: {Owner})");
        
        // Registrar este jugador en LobbyManager si existe
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.RegisterPlayerData(Owner, this);
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Debug.Log($"[PlayerData] Player despawned: {PlayerName}");
    }
}
