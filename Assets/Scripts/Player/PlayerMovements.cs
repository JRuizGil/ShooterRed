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

    [Header("Camara")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookUpAngle = 90f;
    [SerializeField] private float maxLookDownAngle = 90f;
    [SerializeField] public bool invertVertical = false;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private GameObject cameraHolder;

    private CharacterController characterController;
    private Vector3 currentVelocity = Vector3.zero;
    private float verticalVelocity = 0f;
    private float cameraRotationX = 0f;

    // Networked: host escribe, todos leen
    [Networked] public Vector3 NetworkedPosition  { get; set; }
    [Networked] public float   NetworkedRotationY { get; set; }
    [Networked] public NetworkButtons ButtonsPrev { get; set; }

    // =========================================================
    public override void Spawned()
    {
        characterController = GetComponent<CharacterController>();

        verticalVelocity = -2f;
        currentVelocity  = Vector3.zero;
        cameraRotationX  = 0f;

        if (Object.HasStateAuthority)
        {
            NetworkedPosition  = transform.position;
            NetworkedRotationY = transform.eulerAngles.y;
        }

        // CC activo solo en el host — él mueve todos los objetos
        // Los clientes reciben la posición via NetworkedPosition
        characterController.enabled = Object.HasStateAuthority;

        if (Object.HasInputAuthority)
        {
            SetupLocalCamera();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
        else
        {
            DisableRemoteCamera();
        }
    }

    // =========================================================
    // FIXED UPDATE — solo el HOST mueve todos los objetos
    // El cliente NO simula movimiento local (evita doble velocidad)
    // =========================================================
    public override void FixedUpdateNetwork()
    {
        // Solo el host (StateAuthority) mueve los CharacterControllers
        if (!Object.HasStateAuthority) return;

        characterController.enabled = true;

        if (GetInput(out PlayerNetworkInput input))
        {
            // Aplicar rotacion exacta del cliente
            transform.rotation = Quaternion.Euler(0f, input.YawAngle, 0f);

            HandleMovement(input);
            ButtonsPrev = input.Buttons;
        }
        else
        {
            // Sin input: solo gravedad y deceleracion
            HandleGravityOnly();
        }

        // Publicar posicion y rotacion para todos los clientes
        NetworkedPosition  = transform.position;
        NetworkedRotationY = transform.eulerAngles.y;
    }

    // =========================================================
    // RENDER — todos los clientes aplican la posicion del host
    // El host ya tiene la posicion correcta porque el la calcula
    // =========================================================
    public override void Render()
    {
        // El host no necesita aplicar — ya tiene la posicion correcta
        if (Object.HasStateAuthority) return;

        // Tanto el cliente local como los remotos aplican la posicion del host
        transform.position = NetworkedPosition;
        transform.rotation = Quaternion.Euler(0f, NetworkedRotationY, 0f);
    }

    // =========================================================
    // UPDATE — camara solo jugador local
    // =========================================================
    private void Update()
    {
        if (!Object.HasInputAuthority) return;
        HandleCamera();
    }

    // =========================================================
    // MOVIMIENTO
    // =========================================================
    private void HandleMovement(PlayerNetworkInput input)
    {
        Vector3 dir       = GetMoveDirection(input.MoveDirection, input.YawAngle);
        Vector3 targetVel = dir * moveSpeed;

        float curSpd = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
        float tgtSpd = new Vector3(targetVel.x, 0f, targetVel.z).magnitude;
        float acc    = tgtSpd > curSpd ? acceleration : deceleration;

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVel, acc * Runner.DeltaTime);

        HandleGravity(input);

        characterController.Move((currentVelocity + Vector3.up * verticalVelocity) * Runner.DeltaTime);
    }

    // ✅ Usa yawAngle del input — no transform.eulerAngles que tiene retraso
    private Vector3 GetMoveDirection(Vector2 input, float yawAngle)
    {
        Vector3 forward = Quaternion.Euler(0, yawAngle, 0) * Vector3.forward;
        Vector3 right   = Quaternion.Euler(0, yawAngle, 0) * Vector3.right;
        Vector3 dir     = forward * input.y + right * input.x;
        return dir.sqrMagnitude > 0f ? dir.normalized : Vector3.zero;
    }

    private void HandleGravityOnly()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Runner.DeltaTime;
        verticalVelocity  = Mathf.Max(verticalVelocity, gravity * 2f);

        currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Runner.DeltaTime);
        characterController.Move((currentVelocity + Vector3.up * verticalVelocity) * Runner.DeltaTime);
    }

    private void HandleGravity(PlayerNetworkInput input)
    {
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
            if (input.Buttons.WasPressed(ButtonsPrev, PlayerButtons.Jump))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Runner.DeltaTime;
        verticalVelocity  = Mathf.Max(verticalVelocity, gravity * 2f);
    }

    // =========================================================
    // CAMARA
    // =========================================================
    private void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (invertVertical) mouseY = -mouseY;

        transform.Rotate(Vector3.up * mouseX);

        cameraRotationX -= mouseY;
        cameraRotationX  = Mathf.Clamp(cameraRotationX, -maxLookUpAngle, maxLookDownAngle);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    // =========================================================
    // SETUP DE CAMARA
    // =========================================================
    private void SetupLocalCamera()
    {
        Camera ownCamera = null;
        if (cameraHolder != null)
            ownCamera = cameraHolder.GetComponentInChildren<Camera>(true);
        if (ownCamera == null)
            ownCamera = GetComponentInChildren<Camera>(true);

        if (ownCamera != null)
        {
            ownCamera.gameObject.SetActive(true);
            ownCamera.enabled = true;
            ownCamera.tag     = "MainCamera";

            ownCamera.transform.localPosition = Vector3.zero;
            ownCamera.transform.localRotation = Quaternion.identity;
            cameraRotationX = 0f;
            cameraTransform = ownCamera.transform;

            // Desactivar AudioListeners duplicados
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener al in listeners)
                if (al.gameObject != ownCamera.gameObject)
                    al.enabled = false;

            // Desactivar camaras sueltas en escena
            foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (cam == ownCamera) continue;
                if (cam.GetComponentInParent<NetworkObject>() == null)
                    cam.enabled = false;
            }
        }
        else
        {
            Debug.LogError("[PlayerMovements] No hay camara en el prefab.");
        }
    }

    private void DisableRemoteCamera()
    {
        Camera[] cams = GetComponentsInChildren<Camera>(true);
        foreach (Camera cam in cams)
            cam.enabled = false;
    }
}