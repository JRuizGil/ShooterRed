using UnityEngine;
using UnityEngine.UI;

// Controla la barra de vida y su color basado en daño del jugador
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

    // Actualiza el slider y color según la salud actual
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

    // Cambia el valor del slider sin activar callbacks
    public void SetValueWithoutNotify(float value)
    {
        if (healthSlider != null)
            healthSlider.SetValueWithoutNotify(value);
    }

    // Restablece la barra de vida al valor máximo
    public void Reset()
    {
        if (healthSlider != null)
            healthSlider.value = maxHealth;
    }
}
