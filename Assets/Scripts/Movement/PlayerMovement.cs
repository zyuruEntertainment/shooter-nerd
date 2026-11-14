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
    [SerializeField] private FireJoystick fireJoystick;
    [SerializeField] private PlayerCrouch playerCrouch;

    [Header("Crouch Camera")]
    [SerializeField, Min(0f)] private float crouchCameraHeightReduction = 0.5f;
    [SerializeField, Min(0f)] private float crouchCameraLerpSpeed = 10f;

    private CharacterController controller;
    private PlayerPhysics playerPhysics;
    private PlayerDash playerDash;

    private float cameraPitch;
    private Vector3 originalCameraLocalPosition;
    private bool hasOriginalCameraLocalPosition;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerPhysics = GetComponent<PlayerPhysics>();
        playerDash = GetComponent<PlayerDash>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (pcInput == null)
            pcInput = GetComponent<PCInput>();

        if (playerCrouch == null)
            playerCrouch = GetComponent<PlayerCrouch>();

        if (cameraTransform != null)
        {
            originalCameraLocalPosition = cameraTransform.localPosition;
            hasOriginalCameraLocalPosition = true;
        }

        if (fireJoystick == null)
            fireJoystick = FindAnyObjectByType<FireJoystick>();

        // Pastikan penyimpan sensitivitas persisten tersedia.
        SensitivitySettings.EnsureInstance();
    }

    private void Update()
    {
        Vector2 moveInput = Vector2.zero;
        Vector2 lookInput = Vector2.zero;

        bool lookFromMobile = false;
        Vector2 fireLookInput = Vector2.zero;

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

        if (fireJoystick != null)
        {
            Vector2 fireLook = fireJoystick.ConsumeFireLookInput();
            if (fireLook.sqrMagnitude >= 0.0001f)
                fireLookInput = fireLook;
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

        RotatePlayer(lookInput, lookFromMobile, fireLookInput);
        MovePlayer(moveInput);
        UpdateCameraHeight();
    }

    private void UpdateCameraHeight()
    {
        if (cameraTransform == null || !hasOriginalCameraLocalPosition)
            return;

        bool isCrouching = playerCrouch != null && playerCrouch.IsCrouching;
        float targetY = originalCameraLocalPosition.y -
            (isCrouching ? crouchCameraHeightReduction : 0f);

        Vector3 pos = cameraTransform.localPosition;
        pos.y = Mathf.MoveTowards(
            pos.y,
            targetY,
            crouchCameraLerpSpeed * Time.deltaTime
        );
        cameraTransform.localPosition = pos;
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

    private void RotatePlayer(Vector2 lookInput, bool fromMobile, Vector2 fireLookInput)
    {
        var settings = SensitivitySettings.Instance;
        if (settings == null)
            settings = SensitivitySettings.EnsureInstance();

        float rawLook = settings != null ? settings.LookSensitivity : 3f;
        float rawFire = settings != null ? settings.FireSensitivity : 1f;

        float mappedLook = SensitivitySettings.MapLookSensitivity(rawLook);
        float mappedFire = SensitivitySettings.MapLookSensitivity(rawFire);

        float mouseX = 0f;
        float mouseY = 0f;

        if (lookInput.sqrMagnitude >= 0.0001f)
        {
            if (fromMobile)
            {
                float screenRef = Mathf.Max(Screen.height, 1f);
                Vector2 normalized = lookInput / screenRef;
                mouseX += normalized.x * 90f * mappedLook;
                mouseY += normalized.y * 90f * mappedLook;
            }
            else
            {
                mouseX += lookInput.x * 0.1f * mappedLook;
                mouseY += lookInput.y * 0.1f * mappedLook;
            }
        }

        if (fireLookInput.sqrMagnitude >= 0.0001f)
        {
            float screenRef = Mathf.Max(Screen.height, 1f);
            Vector2 normalized = fireLookInput / screenRef;
            mouseX += normalized.x * 90f * mappedFire;
            mouseY += normalized.y * 90f * mappedFire;
        }

        if (Mathf.Abs(mouseX) < 0.0001f && Mathf.Abs(mouseY) < 0.0001f)
            return;

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