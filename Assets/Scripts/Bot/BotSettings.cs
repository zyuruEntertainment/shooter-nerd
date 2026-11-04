using UnityEngine;

public class BotSettings : MonoBehaviour
{
    [Header("Vision")]
    [Tooltip("Maximum distance at which the bot can see the player.")]
    [SerializeField, Min(0f)] private float visionRange = 15f;

    [Tooltip("Field of view in degrees.")]
    [SerializeField, Range(1f, 360f)] private float fieldOfView = 110f;

    [Tooltip("Layers that block vision (walls, obstacles).")]
    [SerializeField] private LayerMask visionObstacleMask = ~0;

    [Tooltip("Layer the player is on.")]
    [SerializeField] private LayerMask playerLayer = ~0;

    [Tooltip("Seconds between vision checks.")]
    [SerializeField, Min(0.05f)] private float visionCheckInterval = 0.2f;

    [Tooltip("How long the bot remembers the last seen position.")]
    [SerializeField, Min(0f)] private float memoryDuration = 3f;

    [Header("Hearing")]
    [Tooltip("Maximum distance at which the bot can hear any sound.")]
    [SerializeField, Min(0f)] private float hearingRange = 25f;

    [Header("Movement")]
    [Tooltip("Walking speed used while patrolling/idle wandering.")]
    [SerializeField, Min(0f)] private float walkSpeed = 3f;

    [Tooltip("Speed used while chasing the player.")]
    [SerializeField, Min(0f)] private float chaseSpeed = 5.5f;

    [Tooltip("Speed used while patrolling.")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2.5f;

    [Tooltip("Acceleration applied to horizontal velocity.")]
    [SerializeField, Min(0f)] private float acceleration = 20f;

    [Tooltip("Deceleration applied when stopping or changing direction.")]
    [SerializeField, Min(0f)] private float deceleration = 25f;

    [Tooltip("Maximum turn speed in degrees per second.")]
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;

    [Tooltip("Air control acceleration.")]
    [SerializeField, Min(0f)] private float airAcceleration = 8f;

    [Tooltip("Air deceleration.")]
    [SerializeField, Min(0f)] private float airDeceleration = 0.5f;

    [Header("Combat")]
    [Tooltip("Distance at which the bot switches to Attack state.")]
    [SerializeField, Min(0f)] private float attackRange = 1.5f;

    [Tooltip("Cooldown between attacks in seconds.")]
    [SerializeField, Min(0f)] private float attackCooldown = 0.8f;

    [Header("Jump")]
    [Tooltip("Distance to player at which jumping becomes considered.")]
    [SerializeField, Min(0f)] private float jumpTriggerDistance = 4f;

    [Tooltip("Cooldown between jumps.")]
    [SerializeField, Min(0f)] private float jumpCooldown = 2f;

    [Tooltip("Probability of jumping when conditions are met.")]
    [SerializeField, Range(0f, 1f)] private float jumpProbability = 0.4f;

    [Header("Dash")]
    [Tooltip("Distance threshold considered 'too close' to the player.")]
    [SerializeField, Min(0f)] private float closeRangeThreshold = 2f;

    [Tooltip("How long the bot must stay too close before dashing.")]
    [SerializeField, Min(0f)] private float dashTriggerDuration = 0.5f;

    [Header("State Timing")]
    [Tooltip("Seconds between state evaluations.")]
    [SerializeField, Min(0.05f)] private float stateEvaluationInterval = 0.15f;

    [Tooltip("Minimum time a state must last before switching (hysteresis).")]
    [SerializeField, Min(0f)] private float minStateDuration = 0.4f;

    [Tooltip("How long to investigate a heard sound location.")]
    [SerializeField, Min(0f)] private float investigateDuration = 3f;

    [Tooltip("How long to search the last known position.")]
    [SerializeField, Min(0f)] private float searchDuration = 4f;

    [Tooltip("How long to stay in Recover state.")]
    [SerializeField, Min(0f)] private float recoverDuration = 0.6f;

    [Header("Reaction")]
    [Tooltip("Minimum reaction delay in seconds.")]
    [SerializeField, Min(0f)] private float reactionTimeMin = 0.08f;

    [Tooltip("Maximum reaction delay in seconds.")]
    [SerializeField, Min(0f)] private float reactionTimeMax = 0.25f;

    [Header("Patrol")]
    [Tooltip("Radius used for random wandering when no patrol points are assigned.")]
    [SerializeField, Min(0f)] private float wanderRadius = 6f;

    [Tooltip("Seconds between picking a new wander point.")]
    [SerializeField, Min(0.5f)] private float wanderInterval = 4f;

    public float VisionRange => visionRange;
    public float FieldOfView => fieldOfView;
    public LayerMask VisionObstacleMask => visionObstacleMask;
    public LayerMask PlayerLayer => playerLayer;
    public float VisionCheckInterval => visionCheckInterval;
    public float MemoryDuration => memoryDuration;
    public float HearingRange => hearingRange;
    public float WalkSpeed => walkSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float PatrolSpeed => patrolSpeed;
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    public float RotationSpeed => rotationSpeed;
    public float AirAcceleration => airAcceleration;
    public float AirDeceleration => airDeceleration;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public float JumpTriggerDistance => jumpTriggerDistance;
    public float JumpCooldown => jumpCooldown;
    public float JumpProbability => jumpProbability;
    public float CloseRangeThreshold => closeRangeThreshold;
    public float DashTriggerDuration => dashTriggerDuration;
    public float StateEvaluationInterval => stateEvaluationInterval;
    public float MinStateDuration => minStateDuration;
    public float InvestigateDuration => investigateDuration;
    public float SearchDuration => searchDuration;
    public float RecoverDuration => recoverDuration;
    public float ReactionTimeMin => reactionTimeMin;
    public float ReactionTimeMax => reactionTimeMax;
    public float WanderRadius => wanderRadius;
    public float WanderInterval => wanderInterval;
}