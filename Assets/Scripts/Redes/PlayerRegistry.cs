using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Registro global de jugadores en red.
/// Permite encontrar el NetworkObject de cualquier PlayerRef rapidamente.
/// Usar: PlayerRegistry.GetPlayer(playerRef)
/// </summary>
public class PlayerRegistry : NetworkBehaviour
{
    public static PlayerRegistry Instance { get; private set; }

    // Mapa local PlayerRef -> NetworkObject
    private Dictionary<PlayerRef, NetworkObject> _players 
        = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        Instance = this;
    }

    /// <summary>
    /// Registrar un jugador al spawnearse.
    /// Llamar desde PlayerState.Spawned()
    /// </summary>
    public static void Register(PlayerRef player, NetworkObject obj)
    {
        if (Instance == null) return;
        Instance._players[player] = obj;
        Debug.Log($"[PlayerRegistry] Registrado: {player}");
    }

    /// <summary>
    /// Eliminar un jugador al desconectarse.
    /// </summary>
    public static void Unregister(PlayerRef player)
    {
        if (Instance == null) return;
        Instance._players.Remove(player);
        Debug.Log($"[PlayerRegistry] Eliminado: {player}");
    }

    /// <summary>
    /// Obtener el NetworkObject de un jugador por su PlayerRef.
    /// </summary>
    public static NetworkObject GetPlayer(PlayerRef player)
    {
        if (Instance == null) return null;
        Instance._players.TryGetValue(player, out var obj);
        return obj;
    }

    /// <summary>
    /// Obtener todos los jugadores registrados.
    /// </summary>
    public static IEnumerable<KeyValuePair<PlayerRef, NetworkObject>> GetAllPlayers()
    {
        if (Instance == null) yield break;
        foreach (var kvp in Instance._players)
            yield return kvp;
    }

    public static int PlayerCount => Instance?._players.Count ?? 0;
}
