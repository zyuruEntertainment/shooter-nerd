
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerPhysics))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    [Header("Acceleration")]
    [SerializeField, Min(0f)]
    private float groundAcceleration = 25f;

    [SerializeField, Min(0f)]
    private float groundDeceleration = 30f;

    [SerializeField, Min(0f)]
    private float airAcceleration = 8f;

    [SerializeField, Min(0f)]
    private float airDeceleration = 0.5f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PCInput pcInput;
    [SerializeField] private MobileInput mobileInput;

    private CharacterController controller;
    private PlayerPhysics playerPhysics;
    private float cameraPitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerPhysics = GetComponent<PlayerPhysics>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (pcInput == null)
            pcInput = GetComponent<PCInput>();
    }

    private void Update()
    {
        Vector2 moveInput = Vector2.zero;
        Vector2 lookInput = Vector2.zero;

        if (mobileInput != null)
        {
            moveInput = mobileInput.MoveInput;
            lookInput = mobileInput.ConsumeLookInput();
        }

        if (pcInput != null)
        {
            if (moveInput.sqrMagnitude < 0.0001f)
                moveInput = pcInput.MoveInput;

            if (lookInput.sqrMagnitude < 0.0001f)
                lookInput = pcInput.ConsumeLookInput();
        }

        RotatePlayer(lookInput);
        MovePlayer(moveInput);
    }

    private void MovePlayer(Vector2 input)
    {
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 direction =
            transform.right * input.x +
            transform.forward * input.y;

        Vector3 targetVelocity = direction * moveSpeed;
        float deltaTime = Time.deltaTime;
        bool grounded = playerPhysics.IsGrounded;

        Vector3 horizontalVelocity =
            playerPhysics.HorizontalVelocity;

        if (input.sqrMagnitude > 0.0001f)
        {
            float acceleration =
                grounded ? groundAcceleration : airAcceleration;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                acceleration * deltaTime
            );
        }
        else
        {
            float deceleration =
                grounded ? groundDeceleration : airDeceleration;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                Vector3.zero,
                deceleration * deltaTime
            );
        }

        playerPhysics.SetHorizontalVelocity(horizontalVelocity);
        playerPhysics.ApplyGravity(deltaTime);

        // Satu-satunya tempat yang menggerakkan CharacterController.
        controller.Move(playerPhysics.Velocity * deltaTime);
    }

    private void RotatePlayer(Vector2 lookInput)
    {
        if (cameraTransform == null)
            return;

        float sensitivity =
            SensitivitySettings.Instance != null
                ? SensitivitySettings.Instance.LookSensitivity
                : 1f;

        transform.Rotate(
            Vector3.up * lookInput.x * sensitivity
        );

        cameraPitch -= lookInput.y * sensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);

        cameraTransform.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }
}
