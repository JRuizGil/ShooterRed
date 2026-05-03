using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovements : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 30f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Cámara")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookUpAngle = 90f;
    [SerializeField] private float maxLookDownAngle = 90f;
    [SerializeField] public bool invertVertical = false;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;

    // Variables locales (no networked — cada cliente simula su propio movimiento)
    private Vector3 currentVelocity = Vector3.zero;
    private float verticalVelocity = 0f;
    private float cameraRotationX = 0f;

    // Networked válidos en Fusion 2
    [Networked] public Vector3 NetworkedVelocity { get; set; }
    [Networked] public NetworkButtons ButtonsPrev { get; set; }

    public override void Spawned()
    {
        characterController = GetComponent<CharacterController>();
        
        if (characterController == null)
        {
            Debug.LogError("[PlayerMovements] CharacterController no encontrado!");
            return;
        }

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Solo bloquear cursor para el jugador local
        if (Object.HasInputAuthority)
        {
            Debug.Log("[PlayerMovements] ¡Este es el jugador local! InputAuthority activa");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Debug.Log("[PlayerMovements] Este es un jugador remoto, sin InputAuthority");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority) 
        {
            Debug.LogWarning($"[PlayerMovements] Player {Object.Id} no tiene InputAuthority");
            return;
        }

        if (GetInput(out PlayerNetworkInput input))
        {
            Debug.Log($"[PlayerMovements] Input recibido: MoveDir={input.MoveDirection}");
            HandleMovement(input);
            ButtonsPrev = input.Buttons;
        }
        else
        {
            Debug.LogWarning("[PlayerMovements] GetInput devolvió false - no hay input disponible");
        }
    }

    private void Update()
    {        
            HandleCamera();
    }

    private void HandleMovement(PlayerNetworkInput input)
    {
        Vector3 moveDirection = GetMoveDirection(input.MoveDirection);

        Vector3 targetVelocity = moveDirection * moveSpeed;
        float currentSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
        float targetSpeed = new Vector3(targetVelocity.x, 0f, targetVelocity.z).magnitude;
        float usedAcceleration = targetSpeed > currentSpeed ? acceleration : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            usedAcceleration * Runner.DeltaTime  // Runner.DeltaTime en FixedUpdateNetwork
        );

        HandleGravity(input);

        Vector3 movement = currentVelocity + Vector3.up * verticalVelocity;
        characterController.Move(movement * Runner.DeltaTime);

        // Sincronizar velocidad para otros clientes
        NetworkedVelocity = currentVelocity;
    }

    private Vector3 GetMoveDirection(Vector2 input)
    {
        if (cameraTransform == null)
            return transform.TransformDirection(new Vector3(input.x, 0f, input.y)).normalized;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;

        Vector3 direction = forward.normalized * input.y + right.normalized * input.x;
        return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
    }

    private void HandleGravity(PlayerNetworkInput input)
    {
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (input.Buttons.WasPressed(ButtonsPrev, PlayerButtons.Jump))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Runner.DeltaTime;
    }

    private void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (invertVertical)
            mouseY = -mouseY;

        transform.Rotate(Vector3.up * mouseX * mouseSensitivity);

        cameraRotationX -= mouseY * mouseSensitivity;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -maxLookUpAngle, maxLookDownAngle);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}