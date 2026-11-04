using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField, Min(0.1f)]
    private float dashDistance = 4f;

    [SerializeField, Min(0.1f)]
    private float dashSpeed = 14f;

    [SerializeField, Min(0f)]
    private float dashCooldown = 0.75f;

    private float remainingDistance;
    private float nextDashTime;
    private Vector3 dashDirection;

    public bool IsDashing { get; private set; }

    public Vector3 DashVelocity => dashDirection * dashSpeed;

    public void Dash()
    {
        Dash(transform.forward);
    }

    public bool Dash(Vector3 direction)
    {
        if (IsDashing || Time.time < nextDashTime)
            return false;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        dashDirection = direction.normalized;
        remainingDistance = dashDistance;

        IsDashing = true;
        nextDashTime = Time.time + dashCooldown;

        return true;
    }

    public float GetDashDistance(float deltaTime)
    {
        if (!IsDashing || deltaTime <= 0f)
            return 0f;

        float distance = Mathf.Min(
            dashSpeed * deltaTime,
            remainingDistance
        );

        remainingDistance -= distance;

        if (remainingDistance <= 0.0001f)
            CancelDash();

        return distance;
    }

    public void UpdateDash(float deltaTime)
    {
        GetDashDistance(deltaTime);
    }

    public void CancelDash()
    {
        remainingDistance = 0f;
        IsDashing = false;
    }
}