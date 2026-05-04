using Fusion;
using UnityEngine;

/// <summary>
/// Estado de un jugador individual en la red
/// Se sincroniza a través de Fusion
/// </summary>
public class PlayerState : NetworkBehaviour
{
    [Networked] public string PlayerName { get; set; }
    [Networked] public int TeamId { get; set; }
    [Networked] public int Kills { get; set; }
    [Networked] public int Deaths { get; set; }
    [Networked] public int Assists { get; set; }
    [Networked] public int Health { get; set; }
    [Networked] public NetworkBool IsAlive { get; set; }
    [Networked] public PlayerRef OwnerPlayer { get; set; }
    
    public override void Spawned()
    {
        OwnerPlayer = Object.InputAuthority;
        IsAlive = true;
        Health = 100;
        
        Debug.Log($"[PlayerState] Player spawned: {PlayerName} (Team: {TeamId})");
    }
    
    public void AddKill(PlayerRef victim)
    {
        if (Object.HasInputAuthority)
        {
            Kills++;
            GameState.Instance?.AddKill(OwnerPlayer, victim);
        }
    }
    
    public void AddDeath()
    {
        if (Object.HasInputAuthority)
        {
            Deaths++;
            IsAlive = false;
        }
    }
    
    public void AddAssist()
    {
        if (Object.HasInputAuthority)
        {
            Assists++;
        }
    }
    
    public void SetHealth(int newHealth)
    {
        if (Object.HasStateAuthority)
        {
            Health = Mathf.Clamp(newHealth, 0, 100);
            if (Health <= 0)
            {
                IsAlive = false;
            }
        }
    }
    
    public int GetKDA()
    {
        return (Kills * 100) + (Assists * 50) - (Deaths * 25);
    }
}
