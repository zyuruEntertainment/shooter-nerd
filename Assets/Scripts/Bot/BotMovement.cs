using UnityEngine;
#if UNITY_AI_NAVIGATION || true
using UnityEngine.AI;
#endif

public class BotMovement : MonoBehaviour
{
    [SerializeField] private BotSettings settings;

    private CharacterController controller;
    private PlayerPhysics physics;
    private PlayerDash playerDash;
    private NavMeshAgent navAgent;

    private Vector3 desiredDestination;
    private bool hasDestination;
    private Vector3 lastNavDestination;
    private bool navDestinationSet;

    private float currentSpeed;

    private void Awake()
    {
        if (settings == null) settings = GetComponent<BotSettings>();
        controller = GetComponent<CharacterController>();
        physics = GetComponent<PlayerPhysics>();
        playerDash = GetComponent<PlayerDash>();
        navAgent = GetComponent<NavMeshAgent>();

        if (navAgent != null)
        {
            // Use agent as path provider only; movement driven by CharacterController.
            navAgent.updatePosition = false;
            navAgent.updateRotation = false;
            navAgent.isStopped = false;
        }
    }

    public void SetDestination(Vector3 destination)
    {
        desiredDestination = destination;
        hasDestination = true;

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            if (!navDestinationSet || (destination - lastNavDestination).sqrMagnitude > 0.25f)
            {
                navAgent.SetDestination(destination);
                lastNavDestination = destination;
                navDestinationSet = true;
            }
        }
    }

    public void Stop()
    {
        if (!hasDestination) return;
        hasDestination = false;
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
            navDestinationSet = false;
        }
    }

    public void Tick(float deltaTime, float desiredSpeed, bool canMove, bool rotateTowardMovement = true)
    {
        if (controller == null || physics == null) return;

        bool grounded = physics.IsGrounded;
        bool dashing = playerDash != null && playerDash.IsDashing;

        Vector3 horizontalVelocity;

        if (dashing)
        {
            horizontalVelocity = playerDash.DashVelocity;
            if (playerDash != null) playerDash.UpdateDash(deltaTime);
        }
        else if (!canMove || !hasDestination)
        {
            // decelerate to stop
            horizontalVelocity = physics.HorizontalVelocity;
            float decel = grounded ? (settings != null ? settings.Deceleration : 25f) : (settings != null ? settings.AirDeceleration : 0.5f);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, decel * deltaTime);
        }
        else
        {
            Vector3 steerDir = ComputeSteerDirection();
            float speed = desiredSpeed;
            Vector3 targetVelocity = steerDir * speed;

            horizontalVelocity = physics.HorizontalVelocity;

            if (steerDir.sqrMagnitude > 0.0001f)
            {
                float accel = grounded ? (settings != null ? settings.Acceleration : 20f) : (settings != null ? settings.AirAcceleration : 8f);
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accel * deltaTime);
            }
            else
            {
                float decel = grounded ? (settings != null ? settings.Deceleration : 25f) : (settings != null ? settings.AirDeceleration : 0.5f);
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, decel * deltaTime);
            }

            // rotation toward movement direction
            if (rotateTowardMovement && steerDir.sqrMagnitude > 0.0001f)
            {
                float rotSpeed = settings != null ? settings.RotationSpeed : 720f;
                Quaternion targetRot = Quaternion.LookRotation(steerDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotSpeed * deltaTime);
            }
        }

        // cancel dash on side collision handled after move
        physics.SetHorizontalVelocity(horizontalVelocity);
        physics.ApplyGravity(deltaTime);

        Vector3 movement = physics.Velocity * deltaTime;
        CollisionFlags flags = controller.Move(movement);

        if (dashing && playerDash != null && (flags & CollisionFlags.Sides) != 0)
        {
            playerDash.CancelDash();
            physics.SetHorizontalVelocity(Vector3.zero);
        }

        // sync nav agent position
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.nextPosition = transform.position;
        }
    }

    private Vector3 ComputeSteerDirection()
    {
        Vector3 toDest;
        if (navAgent != null && navAgent.isOnNavMesh && navAgent.hasPath)
        {
            Vector3 next = navAgent.steeringTarget;
            if ((next - transform.position).sqrMagnitude < 0.01f && navAgent.path.corners.Length > 1)
                next = navAgent.path.corners[1];
            toDest = next - transform.position;
        }
        else
        {
            toDest = desiredDestination - transform.position;
        }
        toDest.y = 0f;
        if (toDest.sqrMagnitude < 0.0001f) return Vector3.zero;
        return toDest.normalized;
    }

    public Vector3 HorizontalVelocity => physics != null ? physics.HorizontalVelocity : Vector3.zero;
    public bool IsGrounded => physics != null ? physics.IsGrounded : false;
}