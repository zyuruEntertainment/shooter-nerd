using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerDash))]
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
    private PlayerDash playerDash;

    private float cameraPitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerPhysics = GetComponent<PlayerPhysics>();
        playerDash = GetComponent<PlayerDash>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (pcInput == null)
            pcInput = GetComponent<PCInput>();

        // Pastikan penyimpan sensitivitas persisten tersedia.
        SensitivitySettings.EnsureInstance();
    }

    private void Update()
    {
        Vector2 moveInput = Vector2.zero;
        Vector2 lookInput = Vector2.zero;

        bool lookFromMobile = false;

        if (mobileInput != null)
        {
            moveInput = mobileInput.MoveInput;
            Vector2 mobileLook = mobileInput.ConsumeLookInput();
            if (mobileLook.sqrMagnitude >= 0.0001f)
            {
                lookInput = mobileLook;
                lookFromMobile = true;
            }
        }

        if (pcInput != null)
        {
            if (moveInput.sqrMagnitude < 0.0001f)
                moveInput = pcInput.MoveInput;

            Vector2 pcLook = pcInput.ConsumeLookInput();
            if (pcLook.sqrMagnitude >= 0.0001f)
            {
                lookInput = pcLook;
                lookFromMobile = false;
            }

            if (pcInput.ConsumeDashInput())
                StartDash(moveInput);
        }

        RotatePlayer(lookInput, lookFromMobile);
        MovePlayer(moveInput);
    }

    private void MovePlayer(Vector2 input)
    {
        float deltaTime = Time.deltaTime;
        bool grounded = playerPhysics.IsGrounded;

        Vector3 horizontalVelocity;

        if (playerDash.IsDashing)
        {
            horizontalVelocity = playerDash.DashVelocity;
        }
        else
        {
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 direction =
                transform.right * input.x +
                transform.forward * input.y;

            Vector3 targetVelocity = direction * moveSpeed;

            horizontalVelocity = playerPhysics.HorizontalVelocity;

            if (input.sqrMagnitude > 0.0001f)
            {
                float acceleration = grounded
                    ? groundAcceleration
                    : airAcceleration;

                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetVelocity,
                    acceleration * deltaTime
                );
            }
            else
            {
                float deceleration = grounded
                    ? groundDeceleration
                    : airDeceleration;

                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    deceleration * deltaTime
                );
            }
        }

        playerPhysics.SetHorizontalVelocity(horizontalVelocity);

        // Gravitasi tetap berjalan selama dash.
        playerPhysics.ApplyGravity(deltaTime);

        Vector3 movement = playerPhysics.Velocity * deltaTime;

        CollisionFlags flags = controller.Move(movement);

        // Batalkan dash jika menabrak dinding.
        if (playerDash.IsDashing &&
            (flags & CollisionFlags.Sides) != 0)
        {
            playerDash.CancelDash();
            playerPhysics.SetHorizontalVelocity(Vector3.zero);
        }
        else if (playerDash.IsDashing)
        {
            // Kurangi jarak dash berdasarkan kecepatan dan waktu.
            playerDash.UpdateDash(deltaTime);
        }
    }

    private void RotatePlayer(Vector2 lookInput, bool fromMobile = false)
    {
        if (lookInput.sqrMagnitude < 0.0001f)
            return;

        var settings = SensitivitySettings.Instance;
        if (settings == null)
            settings = SensitivitySettings.EnsureInstance();

        float sensitivity = settings != null
            ? settings.LookSensitivity
            : 6f;

        // Input mouse PC adalah delta per frame, jadi tetap dikalikan deltaTime.
        // Input sentuh mobile adalah delta posisi sentuhan (PointerEventData.delta)
        // yang tidak boleh dikalikan deltaTime untuk kedua kalinya.
        float mouseX;
        float mouseY;
        if (fromMobile)
        {
            mouseX = lookInput.x * sensitivity;
            mouseY = lookInput.y * sensitivity;
        }
        else
        {
            float deltaTime = Time.deltaTime;
            mouseX = lookInput.x * sensitivity * deltaTime;
            mouseY = lookInput.y * sensitivity * deltaTime;
        }

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation =
                Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void StartDash(Vector2 input)
    {
        Vector3 direction =
            transform.right * input.x +
            transform.forward * input.y;

        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        playerDash.Dash(direction);
    }
}