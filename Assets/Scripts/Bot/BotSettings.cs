using UnityEngine;

public class BotSettings : MonoBehaviour
{
    [Header("Vision")]
    [Tooltip("Maximum distance at which the bot can see the player.")]
    [SerializeField, Min(0f)] private float visionRange = 20f;

    [Tooltip("Field of view in degrees.")]
    [SerializeField, Range(1f, 360f)] private float fieldOfView = 120f;

    [Tooltip("Layers that block vision (walls, obstacles).")]
    [SerializeField] private LayerMask visionObstacleMask = ~0;

    [Tooltip("Layer the player is on.")]
    [SerializeField] private LayerMask playerLayer = ~0;

    [Tooltip("Seconds between vision checks.")]
    [SerializeField, Min(0.05f)] private float visionCheckInterval = 0.1f;

    [Tooltip("How long the bot remembers the last seen position.")]
    [SerializeField, Min(0f)] private float memoryDuration = 3f;

    [Header("Hearing")]
    [Tooltip("Maximum distance at which the bot can hear any sound.")]
    [SerializeField, Min(0f)] private float hearingRange = 25f;

    [Header("Movement")]
    [Tooltip("Walking speed used while patrolling/idle wandering.")]
    [SerializeField, Min(0f)] private float walkSpeed = 3f;

    [Tooltip("Speed used while chasing the player.")]
    [SerializeField, Min(0f)] private float chaseSpeed = 6.5f;

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

    [Header("Engage / Strafe")]
    [Tooltip("Distance at which the bot stops chasing and starts strafing/shooting instead of closing in.")]
    [SerializeField, Min(0f)] private float engageRange = 10f;

    [Tooltip("Speed while strafing left/right around the player.")]
    [SerializeField, Min(0f)] private float strafeSpeed = 4.5f;

    [Tooltip("Seconds before switching strafe direction (fallback when min/max not used).")]
    [SerializeField, Min(0.1f)] private float strafeChangeInterval = 1.5f;

    [Header("Charge (replaces flee - bot charges AT player when damaged)")]
    [Tooltip("Speed while charging at the player after taking damage (higher = harder to escape).")]
    [SerializeField, Min(0f)] private float chargeSpeed = 9f;

    [Tooltip("How long the bot charges at the player after taking damage.")]
    [SerializeField, Min(0f)] private float chargeDuration = 1.5f;

    [Tooltip("How long the bot remembers it was damaged by the player (used to decide Patrol vs Chase).")]
    [SerializeField, Min(0f)] private float damageMemoryDuration = 10f;

    [Tooltip("Minimum seconds between charges (cooldown). Bot tidak akan charge lagi jika masih dalam cooldown.")]
    [SerializeField, Min(0f)] private float chargeCooldown = 4f;

    [Header("Crouch (bot agility - makes bot harder to hit)")]
    [Tooltip("Chance per second to crouch while charging.")]
    [SerializeField, Range(0f, 1f)] private float crouchChance = 0.35f;

    [Tooltip("How long the bot stays crouched.")]
    [SerializeField, Min(0f)] private float crouchDuration = 0.8f;

    [Tooltip("Cooldown between crouches.")]
    [SerializeField, Min(0f)] private float crouchCooldown = 1.2f;

    [Header("Agility")]
    [Tooltip("Rotation speed multiplier while charging/strafing (higher = snappier turns).")]
    [SerializeField, Min(0f)] private float agileRotationMultiplier = 1.6f;

    [Tooltip("Jump chance while charging at the player.")]
    [SerializeField, Range(0f, 1f)] private float chargeJumpChance = 0.6f;

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

    [Header("Tracking (bot aim delay - natural human-like aiming)")]
    [Tooltip("Kecepatan rotasi aim (derajat/detik). Lebih rendah = bot lebih lambat mengikuti pergerakan player.")]
    [SerializeField, Min(0f)] private float aimTrackingSpeed = 360f;

    [Tooltip("Delay reaksi sebelum bot mulai mengikuti pergerakan player (detik). Membuat bot tidak langsung mengarah ke posisi terbaru.")]
    [SerializeField, Min(0f)] private float aimReactionDelay = 0.15f;

    [Tooltip("Faktor smoothing posisi aim (0 = langsung, 1 = sangat lambat). Bot mengikuti posisi player dengan lag.")]
    [SerializeField, Range(0f, 1f)] private float aimSmoothing = 0.5f;

    [Tooltip("Kecepatan pergerakan posisi aim yang diikuti bot (units/detik). Lebih rendah = bot lebih lambat menyesuaikan.")]
    [SerializeField, Min(0f)] private float aimPositionFollowSpeed = 8f;

    [Header("Combat Balance (smarter / harder)")]
    [Tooltip("Minimum seconds between strafe direction changes (unpredictable).")]
    [SerializeField, Min(0.1f)] private float strafeMinInterval = 0.8f;

    [Tooltip("Maximum seconds between strafe direction changes (unpredictable).")]
    [SerializeField, Min(0.1f)] private float strafeMaxInterval = 2f;

    [Tooltip("Seconds to lead the target when aiming (predicts player movement). Higher = harder to dodge.")]
    [SerializeField, Min(0f)] private float leadTime = 0.35f;

    [Tooltip("Chance to jump while in combat (strafe/attack) to avoid being hit.")]
    [SerializeField, Range(0f, 1f)] private float combatJumpChance = 0.5f;

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
    public float EngageRange => engageRange;
    public float StrafeSpeed => strafeSpeed;
    public float StrafeChangeInterval => strafeChangeInterval;
    public float ChargeSpeed => chargeSpeed;
    public float ChargeDuration => chargeDuration;
    public float ChargeCooldown => chargeCooldown;
    public float DamageMemoryDuration => damageMemoryDuration;
    public float CrouchChance => crouchChance;
    public float CrouchDuration => crouchDuration;
    public float CrouchCooldown => crouchCooldown;
    public float AgileRotationMultiplier => agileRotationMultiplier;
    public float ChargeJumpChance => chargeJumpChance;
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
    public float StrafeMinInterval => strafeMinInterval;
    public float StrafeMaxInterval => strafeMaxInterval;
    public float LeadTime => leadTime;
    public float CombatJumpChance => combatJumpChance;

    // Tracking (bot aim delay - natural human-like aiming).
    public float AimTrackingSpeed => aimTrackingSpeed;
    public float AimReactionDelay => aimReactionDelay;
    public float AimSmoothing => aimSmoothing;
    public float AimPositionFollowSpeed => aimPositionFollowSpeed;
}
