using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerPhysics : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [SerializeField, Min(0f)]
    private float maxFallSpeed = 45f;

    [SerializeField, Min(0f)]
    private float groundStickForce = 2f;

    private CharacterController controller;
    private Vector3 velocity;

    public float Gravity => gravity;
    public float MaxFallSpeed => maxFallSpeed;

    public Vector3 Velocity => velocity;

    public float VerticalVelocity => velocity.y;

    public Vector3 HorizontalVelocity =>
        new Vector3(velocity.x, 0f, velocity.z);

    public bool IsGrounded =>
        controller != null && controller.isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void SetVelocity(Vector3 newVelocity)
    {
        velocity = newVelocity;
        ClampFallSpeed();
    }

    public void SetVerticalVelocity(float verticalVelocity)
    {
        velocity.y = verticalVelocity;
        ClampFallSpeed();
    }

    public void SetHorizontalVelocity(Vector3 horizontalVelocity)
    {
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
    }

    public void ApplyGravity(float deltaTime)
    {
        if (controller == null)
            return;

        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -groundStickForce;
            return;
        }

        velocity.y += gravity * deltaTime;
        ClampFallSpeed();
    }

    private void ClampFallSpeed()
    {
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
    }

    public void StopVerticalVelocity()
    {
        velocity.y = 0f;
    }

    public void StopAllVelocity()
    {
        velocity = Vector3.zero;
    }
}