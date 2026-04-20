using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Configuración de la Cámara")]
    [SerializeField] private float mouseSensitivity = 100f; // Sensibilidad del mouse
    [SerializeField] private float maxVerticalAngle = 90f;   // Límite superior de rotación vertical
    [SerializeField] private float minVerticalAngle = -90f;  // Límite inferior de rotación vertical
    [SerializeField] private Transform playerBody;           // Referencia al cuerpo del jugador (para rotación horizontal)

    private float xRotation = 0f; // Rotación acumulada en el eje X (vertical)

    void Start()
    {
        // Bloquear el cursor en el centro de la pantalla y ocultarlo
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Obtener entrada del mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotación vertical (solo afecta a la cámara)
        xRotation -= mouseY; // Invertir porque el mouse Y es positivo hacia abajo
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle); // Limitar
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotación horizontal (afecta al cuerpo del jugador para que gire)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }

    // Método opcional para desbloquear el cursor (útil en menús o pausa)
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}