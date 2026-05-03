using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida sincronizada con el sistema de daño del jugador
/// </summary>
public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.red;
    [SerializeField] private float maxHealth = 4f;

    private PlayerHealth playerHealth;
    private Canvas parentCanvas;

    private void Start()
    {
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        if (fillImage == null && healthSlider != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();

        parentCanvas = GetComponentInParent<Canvas>();

        // Encontrar PlayerHealth en el padre
        playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        Debug.Log("[HealthBar] Health bar initialized");
    }

    private void Update()
    {
        if (playerHealth == null)
            return;

        UpdateHealthDisplay();
    }

    /// <summary>
    /// Actualizar la visualización de la barra de vida
    /// </summary>
    private void UpdateHealthDisplay()
    {
        int hitsRemaining = playerHealth.GetHitsRemaining();
        bool isAlive = playerHealth.GetIsAlive();

        if (healthSlider != null)
        {
            healthSlider.value = hitsRemaining;
        }

        // Cambiar color según daño
        if (fillImage != null)
        {
            float healthPercent = hitsRemaining / maxHealth;
            fillImage.color = Color.Lerp(damagedColor, healthyColor, healthPercent);
        }

        // Desactivar si está muerto
        if (!isAlive && parentCanvas != null)
        {
            parentCanvas.enabled = false;
        }
        else if (isAlive && parentCanvas != null)
        {
            parentCanvas.enabled = true;
        }
    }

    /// <summary>
    /// Establecer el valor sin disparar notificaciones
    /// </summary>
    public void SetValueWithoutNotify(float value)
    {
        if (healthSlider != null)
            healthSlider.SetValueWithoutNotify(value);
    }

    /// <summary>
    /// Reiniciar la barra de vida
    /// </summary>
    public void Reset()
    {
        if (healthSlider != null)
            healthSlider.value = maxHealth;
    }
}
