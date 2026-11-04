using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSpriteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerPhysics playerPhysics;
    [SerializeField] private PlayerCrouch playerCrouch;
    [SerializeField] private PCInput pcInput;
    [SerializeField] private MobileInput mobileInput;

    [Header("Player Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite runSprite;
    [SerializeField] private Sprite runSprite2;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite crouchSprite;
    [SerializeField] private Sprite shootSprite;
    [SerializeField] private Sprite crouchShootSprite;

    [Header("Run Animation")]
    [SerializeField, Min(0.01f)] private float runFrameDuration = 0.12f;

    private bool isShooting;
    private Sprite currentSprite;
    private float runFrameTimer;
    private bool showSecondRunFrame;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (playerPhysics == null)
            playerPhysics = GetComponent<PlayerPhysics>();

        if (playerCrouch == null)
            playerCrouch = GetComponent<PlayerCrouch>();

        if (pcInput == null)
            pcInput = GetComponent<PCInput>();

        if (mobileInput == null)
            mobileInput = FindAnyObjectByType<MobileInput>();

        if (spriteRenderer == null)
            Debug.LogError("PlayerSpriteController: SpriteRenderer tidak ditemukan.", this);
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null)
            return;

        bool grounded = playerPhysics == null || playerPhysics.IsGrounded;
        bool crouching = playerCrouch != null && playerCrouch.IsCrouching;
        bool moving = GetMoveInput().sqrMagnitude > 0.01f;
        bool jumping = !grounded ||
            (playerPhysics != null && playerPhysics.VerticalVelocity > 0.1f);

        Sprite nextSprite;

        if (jumping)
        {
            ResetRunAnimation();
            nextSprite = jumpSprite;
        }
        else if (crouching && isShooting)
        {
            ResetRunAnimation();
            nextSprite = crouchShootSprite;
        }
        else if (crouching)
        {
            ResetRunAnimation();
            nextSprite = crouchSprite;
        }
        else if (isShooting)
        {
            ResetRunAnimation();
            nextSprite = shootSprite;
        }
        else if (moving)
        {
            nextSprite = GetRunAnimationSprite();
        }
        else
        {
            ResetRunAnimation();
            nextSprite = idleSprite;
        }

        SetSprite(nextSprite);
    }

    private Sprite GetRunAnimationSprite()
    {
        if (runSprite2 == null)
            return runSprite;

        runFrameTimer += Time.deltaTime;

        if (runFrameTimer >= runFrameDuration)
        {
            runFrameTimer = 0f;
            showSecondRunFrame = !showSecondRunFrame;
        }

        return showSecondRunFrame ? runSprite2 : runSprite;
    }

    private void ResetRunAnimation()
    {
        runFrameTimer = 0f;
        showSecondRunFrame = false;
    }

    private void SetSprite(Sprite nextSprite)
    {
        if (nextSprite != null && nextSprite != currentSprite)
        {
            spriteRenderer.sprite = nextSprite;
            currentSprite = nextSprite;
        }
    }

    private Vector2 GetMoveInput()
    {
        if (mobileInput != null && mobileInput.MoveInput.sqrMagnitude > 0.0001f)
            return mobileInput.MoveInput;

        return pcInput != null ? pcInput.MoveInput : Vector2.zero;
    }

    public void SetShooting(bool value)
    {
        isShooting = value;
    }
}
