using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovements : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 30f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private Vector3 currentVelocity = Vector3.zero;
    private float verticalVelocity = 0f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector3 moveDirection = GetMoveDirection(input);

        Vector3 targetVelocity = moveDirection * moveSpeed;
        float currentSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
        float targetSpeed = new Vector3(targetVelocity.x, 0f, targetVelocity.z).magnitude;
        float usedAcceleration = targetSpeed > currentSpeed ? acceleration : deceleration;

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, usedAcceleration * Time.deltaTime);

        HandleGravity();

        Vector3 movement = currentVelocity + Vector3.up * verticalVelocity;
        characterController.Move(movement * Time.deltaTime);
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

    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
    }
}
