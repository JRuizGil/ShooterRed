using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    /// <summary>
    /// Propietario de red del jugador (identifica quién controla este objeto)
    /// </summary>
    [Networked]
    public PlayerRef OwnerPlayer { get; set; }

    /// <summary>
    /// Salud actual del jugador (sincronizada por red)
    /// </summary>
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

    //[SerializeField] private HealthBar healthBar;
    [SerializeField] private GameObject hitVfx;

    /// <summary>
    /// Llamado cuando el objeto entra a la simulación de red
    /// Aquí es seguro acceder a propiedades [Networked]
    /// </summary>
    public override void Spawned()
    {
        Debug.Log($"[PlayerState] Player spawned: {OwnerPlayer}, Health: {Health}");
        
        // Sincronizar visualización inicial
        RefreshHealthVisuals();
        
        // Resolver referencias de componentes dependientes
        ResolveComponentReferences();
    }

    /// <summary>
    /// Llamado cuando el objeto sale de la simulación de red
    /// </summary>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Debug.Log($"[PlayerState] Player despawned: {OwnerPlayer}");
        
        if (hitVfx != null)
        {
            hitVfx.SetActive(false);
        }
    }

    /// <summary>
    /// Callback cuando la salud cambia (por OnChangedRender)
    /// </summary>
    private void OnHealthChanged()
    {
        Debug.Log($"[PlayerState] Health changed to: {Health}");
        RefreshHealthVisuals();
    }

    /// <summary>
    /// Actualizar la visualización de salud
    /// </summary>
    private void RefreshHealthVisuals()
    {
        // healthBar.SetValueWithoutNotify(Health);
        Debug.Log($"[PlayerState] Refreshing health visuals: {Health}");
    }

    /// <summary>
    /// Reproducir feedback de daño local
    /// </summary>
    private void PlayLocalDamageFeedback()
    {
        if (hitVfx != null)
        {
            hitVfx.SetActive(true);
        }
    }

    /// <summary>
    /// Resolver referencias de componentes internos
    /// Se llama en Spawned() para asegurar que todo esté listo
    /// </summary>
    private void ResolveComponentReferences()
    {
        // Aquí puedes conectar con PlayerMovements, PlayerHabilities, etc.
        // cuando estén implementados
    }
}