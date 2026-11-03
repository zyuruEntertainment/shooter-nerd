
using UnityEngine;

[RequireComponent(typeof(PlayerPhysics))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField, Min(0f)]
    private float jumpHeight = 1.5f;

    [SerializeField, Min(0f)]
    private float jumpBufferTime = 0.15f;

    [Header("References")]
    [SerializeField] private PCInput pcInput;
    [SerializeField] private PlayerPhysics playerPhysics;

    private float jumpBufferCounter;

    private void Awake()
    {
        if (pcInput == null)
            pcInput = GetComponent<PCInput>();

        if (playerPhysics == null)
            playerPhysics = GetComponent<PlayerPhysics>();
    }

    private void Update()
    {
        if (pcInput != null && pcInput.ConsumeJumpInput())
            Jump();

        if (jumpBufferCounter <= 0f)
            return;

        jumpBufferCounter -= Time.deltaTime;

        if (!playerPhysics.IsGrounded)
            return;

        float jumpVelocity = Mathf.Sqrt(
            jumpHeight * -2f * playerPhysics.Gravity
        );

        playerPhysics.SetVerticalVelocity(jumpVelocity);
        jumpBufferCounter = 0f;
    }

    // Dipanggil keyboard maupun UI Button.
    public void Jump()
    {
        if (playerPhysics == null)
        {
            Debug.LogError("PlayerPhysics tidak ditemukan.", this);
            return;
        }

        jumpBufferCounter = jumpBufferTime;
    }
}
