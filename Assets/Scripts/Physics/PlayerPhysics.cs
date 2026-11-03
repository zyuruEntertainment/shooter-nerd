
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerPhysics : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private Vector3 velocity;

    public float Gravity => gravity;
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

    // PlayerJump mengatur kecepatan vertikal.
    public void SetVerticalVelocity(float verticalVelocity)
    {
        velocity.y = verticalVelocity;
    }

    // PlayerMovement mengatur momentum horizontal.
    public void SetHorizontalVelocity(Vector3 horizontalVelocity)
    {
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
    }

    // Memperbarui gravitasi tanpa memindahkan karakter.
    public void ApplyGravity(float deltaTime)
    {
        if (controller == null)
            return;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * deltaTime;
    }
}
