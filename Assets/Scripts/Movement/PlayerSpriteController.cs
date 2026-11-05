using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSpriteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerPhysics playerPhysics;
    [SerializeField] private PlayerCrouch playerCrouch;
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

    public bool IsShooting => isShooting;
    public bool IsMoving { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsCrouching { get; private set; }

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

    private void UpdateCharacterState()
    {
        if (playerPhysics != null)
        {
            Vector3 horizontalVelocity =
                playerPhysics.HorizontalVelocity;

            float threshold = movementThreshold;

            IsMoving =
                horizontalVelocity.sqrMagnitude >
                threshold * threshold;

            IsJumping =
                !playerPhysics.IsGrounded ||
                playerPhysics.VerticalVelocity > 0.1f;
        }
        else
        {
            IsMoving = false;
            IsJumping = false;
        }

        bool playerCrouching =
            playerCrouch != null &&
            playerCrouch.IsCrouching;

        IsCrouching = playerCrouching || externalCrouch;
    }

    private Sprite ResolveSprite()
    {
        bool shooting = IsShooting ||
            (botCombat != null && botCombat.IsAttackAnimating);

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

    // Public API: shooting state.
    public void SetShooting(bool value)
    {
        isShooting = value;
    }

    // Public API: optional crouch state.
    // Useful for future bot crouch behavior.
    public void SetCrouching(bool value)
    {
        externalCrouch = value;
    }

    // Optional runtime sprite updates.
    public void SetIdleSprite(Sprite sprite)
    {
        idleSprite = sprite;
    }

    public void SetRunSprites(Sprite first, Sprite second)
    {
        runSprite = first;
        runSprite2 = second;
        ResetRunAnimation();
    }

    public void SetJumpSprite(Sprite sprite)
    {
        jumpSprite = sprite;
    }

    public void SetShootSprites(Sprite normal, Sprite crouched)
    {
        shootSprite = normal;
        crouchShootSprite = crouched;
    }
}