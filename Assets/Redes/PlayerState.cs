using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

    //[SerializeField] private HealthBar healthBar;
    [SerializeField] private GameObject hitVfx;

    public override void Spawned()
    {
        RefreshHealthVisuals();
    }

    private void OnHealthChanged()
    {
        RefreshHealthVisuals();
    }

    private void RefreshHealthVisuals()
    {
        // healthBar.SetValueWithoutNotify(Health);
    }

    private void PlayLocalDamageFeedback()
    {
        hitVfx.SetActive(true);
    }
}