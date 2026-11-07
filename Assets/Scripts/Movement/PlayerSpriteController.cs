using UnityEngine;

/// <summary>
/// Sprite controller that works for both Player and Bot characters.
/// Uses PlayerPhysics state (velocity, grounded) as the sole source of movement data,
/// eliminating dependency on player input (PCInput, MobileInput).
/// </summary>
[DisallowMultipleComponent]
public class PlayerSpriteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerPhysics playerPhysics;
    [SerializeField] private PlayerCrouch playerCrouch;

    [Header("Bot Override")]
    [SerializeField] private BotCombat botCombat;

    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite runSprite;
    [SerializeField] private Sprite runSprite2;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite crouchSprite;
    [SerializeField] private Sprite shootSprite;
    [SerializeField] private Sprite crouchShootSprite;

    [Header("Movement Detection")]
    [SerializeField, Min(0f)]
    private float movementThreshold = 0.1f;

    [Header("Run Animation")]
    [SerializeField, Min(0.01f)]
    private float runFrameDuration = 0.12f;

    private Sprite currentSprite;

    private float runFrameTimer;
    private bool showSecondRunFrame;

    private bool isShooting;
    private bool externalCrouch;

    // Public API properties - read-only from outside, set internally
    public bool IsShooting => isShooting;
    public bool IsMoving { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsCrouching { get; private set; }

    // --- Animation state setters (public API for bot/player) ---
    /// <summary>
    /// Call to set shooting state. Use botCombat.IsAttackAnimating or player shooting state.
    /// </summary>
    public void SetShooting(bool value)
    {
        isShooting = value;
    }

    /// <summary>
    /// Call to set external crouch state. For bot, this can be used if bot implements crouch behavior.
    /// Player uses PlayerCrouch component automatically.
    /// </summary>
    public void SetCrouching(bool value)
    {
        externalCrouch = value;
    }

    private void Awake()
    {
        CacheReferences();
        ValidateReferences();
    }

    private void CacheReferences()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (playerPhysics == null)
            playerPhysics = GetComponent<PlayerPhysics>();

        if (playerCrouch == null)
            playerCrouch = GetComponent<PlayerCrouch>();

        // botCombat is optional - bot may have its own combat component
        if (botCombat == null)
            botCombat = GetComponent<BotCombat>();
    }

    private void ValidateReferences()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError(
                "PlayerSpriteController: SpriteRenderer missing.",
                this
            );

            enabled = false;
            return;
        }

        if (playerPhysics == null)
        {
            Debug.LogWarning(
                "PlayerSpriteController: PlayerPhysics missing. " +
                "Movement animation detection will be unavailable.",
                this
            );
        }
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null)
            return;

        UpdateCharacterState();

        Sprite nextSprite = ResolveSprite();

        ApplySprite(nextSprite);
    }

    /// <summary>
    /// Reads movement state from PlayerPhysics.
    /// Does NOT use any input (PCInput/MobileInput) - suitable for both player and bot.
    /// </summary>
    private void UpdateCharacterState()
    {
        // Use PlayerPhysics as the sole source of movement data
        if (playerPhysics != null)
        {
            // Horizontal velocity magnitude for movement detection
            Vector3 horizontalVelocity = playerPhysics.HorizontalVelocity;
            float horizontalSpeed = horizontalVelocity.magnitude;

            IsMoving = horizontalSpeed > movementThreshold;

            // Vertical velocity and grounded state for jump detection
            IsJumping = !playerPhysics.IsGrounded ||
                        playerPhysics.VerticalVelocity > 0.1f;
        }
        else
        {
            IsMoving = false;
            IsJumping = false;
        }

        // Crouch state: PlayerCrouch for player, externalCrouch for bot override
        bool playerCrouching = false;
        if (playerCrouch != null)
        {
            playerCrouching = playerCrouch.IsCrouching;
        }

        IsCrouching = playerCrouching || externalCrouch;
    }

    private Sprite ResolveSprite()
    {
        bool shooting = isShooting ||
                        (botCombat != null && botCombat.IsAttackAnimating);

        // Priority order (highest to lowest):
        // 1. Jump
        // 2. Crouch Shoot
        // 3. Crouch
        // 4. Shoot
        // 5. Run
        // 6. Idle

        if (IsJumping)
        {
            ResetRunAnimation();
            return jumpSprite;
        }

        if (IsCrouching && shooting)
        {
            ResetRunAnimation();
            return crouchShootSprite;
        }

        if (IsCrouching)
        {
            ResetRunAnimation();
            return crouchSprite;
        }

        if (shooting)
        {
            ResetRunAnimation();
            return shootSprite;
        }

        if (IsMoving)
        {
            return GetRunAnimationSprite();
        }

        ResetRunAnimation();
        return idleSprite;
    }

    private Sprite GetRunAnimationSprite()
    {
        if (runSprite2 == null)
            return runSprite;

        float duration = Mathf.Max(0.01f, runFrameDuration);

        runFrameTimer += Time.deltaTime;

        if (runFrameTimer >= duration)
        {
            int frameSteps = Mathf.FloorToInt(
                runFrameTimer / duration
            );

            runFrameTimer -= frameSteps * duration;

            // Toggle between runSprite and runSprite2 every frame
            if ((frameSteps & 1) != 0)
                showSecondRunFrame = !showSecondRunFrame;
        }

        return showSecondRunFrame
            ? runSprite2
            : runSprite;
    }

    private void ResetRunAnimation()
    {
        runFrameTimer = 0f;
        showSecondRunFrame = false;
    }

    private void ApplySprite(Sprite nextSprite)
    {
        if (nextSprite == null)
            return;

        if (currentSprite == nextSprite)
            return;

        spriteRenderer.sprite = nextSprite;
        currentSprite = nextSprite;
    }

    // --- Public API for external state control ---

    /// <summary>
    /// Set the shooting state externally.
    /// Bot: use BotCombat.IsAttackAnimating value.
    /// Player: use your own shooting logic.
    /// </summary>
    public void SetShootingState(bool value)
    {
        isShooting = value;
    }

    /// <summary>
    /// Set external crouch state.
    /// Useful when bot implements crouch behavior in the future.
    /// Player typically uses PlayerCrouch component automatically.
    /// </summary>
    public void SetCrouchState(bool value)
    {
        externalCrouch = value;
    }

    /// <summary>
    /// Set jump sprite override (optional).
    /// </summary>
    public void SetJumpSprite(Sprite sprite)
    {
        jumpSprite = sprite;
    }

    /// <summary>
    /// Set idle sprite override (optional).
    /// </summary>
    public void SetIdleSprite(Sprite sprite)
    {
        idleSprite = sprite;
    }

    /// <summary>
    /// Set run sprites override (optional).
    /// </summary>
    public void SetRunSprites(Sprite first, Sprite second)
    {
        runSprite = first;
        runSprite2 = second;
        ResetRunAnimation();
    }

    /// <summary>
    /// Set shoot sprites override (optional).
    /// </summary>
    public void SetShootSprites(Sprite normal, Sprite crouched)
    {
        shootSprite = normal;
        crouchShootSprite = crouched;
    }

    /// <summary>
    /// Get the current horizontal velocity from PlayerPhysics.
    /// Useful for bot movement debugging or other systems.
    /// </summary>
    public Vector3 GetHorizontalVelocity()
    {
        return playerPhysics != null ? playerPhysics.HorizontalVelocity : Vector3.zero;
    }

    /// <summary>
    /// Get the vertical velocity from PlayerPhysics.
    /// </summary>
    public float GetVerticalVelocity()
    {
        return playerPhysics != null ? playerPhysics.VerticalVelocity : 0f;
    }

    /// <summary>
    /// Get the grounded state from PlayerPhysics.
    /// </summary>
    public bool GetIsGrounded()
    {
        return playerPhysics != null ? playerPhysics.IsGrounded : false;
    }
}