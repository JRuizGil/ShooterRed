using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    // Identificador del jugador que controla este objeto en red
    [Networked]
    public PlayerRef OwnerPlayer { get; set; }

    // Nombre del jugador sincronizado en la red
    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    // Puntos de vida del jugador sincronizados en red
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

    //[SerializeField] private HealthBar healthBar;
    [SerializeField] private GameObject hitVfx;

    // Se ejecuta cuando el jugador entra en la simulación de red
    public override void Spawned()
    {
        Debug.Log($"[PlayerState] Player spawned: {OwnerPlayer}, Health: {Health}");
        
        // Sincronizar visualización inicial
        RefreshHealthVisuals();
        
        // Resolver referencias de componentes dependientes
        ResolveComponentReferences();
    }

    // Se ejecuta cuando el jugador sale de la simulación de red
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Debug.Log($"[PlayerState] Player despawned: {OwnerPlayer}");
        
        if (hitVfx != null)
        {
            hitVfx.SetActive(false);
        }
    }

    // Se dispara cuando la salud cambia en red
    private void OnHealthChanged()
    {
        Debug.Log($"[PlayerState] Health changed to: {Health}");
        RefreshHealthVisuals();
    }

    // Se dispara cuando el nombre del jugador cambia en red
    private void OnPlayerNameChanged()
    {
        Debug.Log($"[PlayerState] Player name changed to: {PlayerName}");
    }

    // Sincroniza el valor de salud con los elementos visuales
    private void RefreshHealthVisuals()
    {
        // healthBar.SetValueWithoutNotify(Health);
        Debug.Log($"[PlayerState] Refreshing health visuals: {Health}");
    }

    // Activa el efecto visual de daño en el cliente
    private void PlayLocalDamageFeedback()
    {
        if (hitVfx != null)
        {
            hitVfx.SetActive(true);
        }
    }

    // Busca y conecta referencias de componentes dependientes del jugador
    private void ResolveComponentReferences()
    {
        // Aquí puedes conectar con PlayerMovements, PlayerHabilities, etc.
        // cuando estén implementados
    }
}