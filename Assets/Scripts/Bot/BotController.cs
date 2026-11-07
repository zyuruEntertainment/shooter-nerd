using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BotSettings settings;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Vision Eye (optional)")]
    [SerializeField] private Transform eyePoint;

    private BotVision vision;
    private BotHearing hearing;
    private BotPerception perception;
    private BotMovement movement;
    private BotCombat combat;
    private BotRangedCombat rangedCombat;
    private BotJumpController jumpController;
    private BotDashController dashController;
    private BotStateMachine stateMachine;

    private CharacterController controller;
    private PlayerPhysics physics;
    private CombatHealth health;
    private PlayerSpriteController spriteController;

    private float visionTimer;
    private float stateEvalTimer;
    private float reactionTimer;
    private float wanderTimer;
    private PerceptionStatus lastPerceptionStatus;
    private Vector3 wanderTarget;
    private bool hasWanderTarget;

    private int patrolIndex;
    private Vector3 lastPlayerPosition;

    private bool justDashed;
    private bool isFleeing;
    private float fleeTimer;
    private float strafeTimer;
    private float chaseJumpTimer;
    private int strafeDirection = 1;

    private float lastDamageTime = -999f;

    public bool HasBeenDamagedRecently =>
        settings != null && Time.time - lastDamageTime <= settings.DamageMemoryDuration;

    public BotSettings Settings => settings;
    public BotPerception Perception => perception;
    public BotStateMachine StateMachine => stateMachine;
    public bool HasPatrolPoints => patrolPoints != null && patrolPoints.Length > 0;
    public bool JustDashed => justDashed;
    public bool IsFleeing => isFleeing;
    public void ClearJustDashed() { justDashed = false; }

    public float DistanceToPlayer
    {
        get
        {
            if (playerTarget == null) return float.MaxValue;
            return Vector3.Distance(transform.position, playerTarget.position);
        }
    }

    private void Awake()
    {
        if (settings == null) settings = GetComponent<BotSettings>();
        if (settings == null)
        {
            Debug.LogError("BotController: BotSettings missing.", this);
            enabled = false;
            return;
        }

        vision = GetComponent<BotVision>();
        hearing = GetComponent<BotHearing>();
        perception = GetComponent<BotPerception>();
        movement = GetComponent<BotMovement>();
        combat = GetComponent<BotCombat>();
        rangedCombat = GetComponent<BotRangedCombat>();
        jumpController = GetComponent<BotJumpController>();
        dashController = GetComponent<BotDashController>();
        stateMachine = GetComponent<BotStateMachine>();

        controller = GetComponent<CharacterController>();
        physics = GetComponent<PlayerPhysics>();
        health = GetComponent<CombatHealth>();
        spriteController = GetComponent<PlayerSpriteController>();

        if (stateMachine == null) stateMachine = gameObject.AddComponent<BotStateMachine>();

        // ensure perception references
        if (perception != null)
        {
            // reflection not needed; perception uses GetComponent in Awake already
        }

        if (vision != null)
        {
            if (eyePoint != null) vision.SetEye(eyePoint);
        }

        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }

        if (vision != null) vision.SetTarget(playerTarget);
        if (hearing != null) { /* hearing uses events */ }

        // Subscribe to damage so bot can flee when shot.
        if (health != null)
            health.OnTakeDamage += OnBotTakeDamage;

        visionTimer = settings != null ? settings.VisionCheckInterval : 0.2f;
        stateEvalTimer = settings != null ? settings.StateEvaluationInterval : 0.15f;
        reactionTimer = RandomReactionDelay();
        wanderTimer = 0f;

        justDashed = false;
        isFleeing = false;
        fleeTimer = 0f;
        strafeTimer = 0f;
    }

    private void OnBotTakeDamage(float amount)
    {
        // When shot, interrupt current behavior and flee for a set duration.
        isFleeing = true;
        fleeTimer = settings != null ? settings.FleeDuration : 2f;
        lastDamageTime = Time.time; // ingat bahwa player sudah pernah menembak
        justDashed = false; // allow dash on next evaluate
    }

    private void Start()
    {
        // BotStateMachine resolves its controller reference in its own Awake.
    }

    private void Update()
    {
        if (settings == null) return;

        float dt = Time.deltaTime;

        // Vision check on interval
        visionTimer -= dt;
        if (visionTimer <= 0f && vision != null)
        {
            vision.CheckVision();
            visionTimer = settings.VisionCheckInterval;
        }

        // Perception refresh (cheap)
        if (perception != null) perception.Refresh();

        // Reaction delay when player first becomes visible.
        if (perception != null && perception.Status == PerceptionStatus.Visible && lastPerceptionStatus != PerceptionStatus.Visible)
        {
            reactionTimer = RandomReactionDelay();
        }
        if (perception != null) lastPerceptionStatus = perception.Status;
        // State evaluation on interval
        stateEvalTimer -= dt;
        if (stateEvalTimer <= 0f)
        {
            if (stateMachine != null) stateMachine.Evaluate();
            stateEvalTimer = settings.StateEvaluationInterval;
        }

        // Reaction delay timer (small) - delays target position update
        if (reactionTimer > 0f) reactionTimer -= dt;

        // Update movement destination based on state
        UpdateMovementDestination();

        // Movement tick every frame
        if (movement != null)
        {
            float speed = GetCurrentSpeed();
            bool canMove = CanMoveInCurrentState();
            // Saat Strafe dan Flee: bot hanya geser posisi, jangan memutar tubuh ke arah gerak.
            // Rotasi tangani oleh BotRangedCombat (aim lock ke player).
            bool rotateTowardMovement = stateMachine == null ||
                (stateMachine.CurrentState != BotState.Strafe &&
                 stateMachine.CurrentState != BotState.Flee);
            movement.Tick(dt, speed, canMove, rotateTowardMovement);
        }

        // Jump / dash attempts
        BotState st = stateMachine != null ? stateMachine.CurrentState : BotState.Idle;
        bool chasing = st == BotState.Chase || st == BotState.Attack || st == BotState.Strafe;
        bool fleeing = st == BotState.Flee;
        if ((chasing || fleeing) && playerTarget != null)
        {
            float dist = Vector3.Distance(transform.position, playerTarget.position);
            Vector3 toPlayer = playerTarget.position - transform.position;

            if (jumpController != null)
            {
                bool clearance = jumpController.HasForwardClearance(transform.forward, 1.5f);
                jumpController.TryJump(chasing, dist, clearance);
            }

            if (dashController != null)
            {
                if (fleeing)
                {
                    // Dash away from player while fleeing.
                    Vector3 away = transform.position - playerTarget.position;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
                    dashController.TryDash(away, true, forceDash: true);
                }
                else
                {
                    dashController.TryDash(toPlayer, true);
                }
            }
        }

        // Lompat sesekali saat mengejar (tidak dibatasi jarak ke player).
        if (st == BotState.Chase && playerTarget != null && jumpController != null && physics.IsGrounded)
        {
            chaseJumpTimer -= dt;
            if (chaseJumpTimer <= 0f)
            {
                jumpController.TryChaseJump();
                chaseJumpTimer = Random.Range(3f, 6f);
            }
        }

        // Track dash rising edge
        TrackDashRisingEdge();

        // Aktifkan BotRangedCombat (raycast + aim lock) hanya saat tidak Flee.
        // Saat Flee: bot hanya lari, tidak menembak.
        if (rangedCombat != null)
            rangedCombat.enabled = st != BotState.Flee;

        // Combat + sprite shooting feedback.
        bool isShooting = false;
        if (st == BotState.Attack && combat != null && playerTarget != null)
        {
            isShooting = combat.TryAttack(playerTarget);
        }
        else if (st == BotState.Strafe && playerTarget != null)
        {
            // BotRangedCombat menembak secara independen saat Strafe.
            // Saat Flee, rangedCombat.enabled sudah di-set false di atas, jadi tidak akan tembak nyata.
            isShooting = true;
        }

        if (spriteController != null)
            spriteController.SetShootingState(isShooting);

        // Recover handling: if recover state and timer done, state machine handles next eval
    }

    private bool prevDashing;
    private void TrackDashRisingEdge()
    {
        var pd = GetComponent<PlayerDash>();
        if (pd == null) return;
        bool now = pd.IsDashing;
        if (now && !prevDashing)
        {
            justDashed = true;
        }
        prevDashing = now;
    }

    private void UpdateMovementDestination()
    {
        if (movement == null) return;
        if (stateMachine == null) return;

        BotState st = stateMachine.CurrentState;
        switch (st)
        {
            case BotState.Idle:
                movement.Stop();
                // small wander occasionally
                HandleWander();
                break;
            case BotState.Patrol:
                HandlePatrol();
                break;
            case BotState.Investigate:
                if (perception != null && perception.HasTarget)
                    movement.SetDestination(perception.TargetPosition);
                break;
            case BotState.Chase:
                if (playerTarget != null && reactionTimer <= 0f)
                {
                    movement.SetDestination(playerTarget.position);
                }
                else if (playerTarget != null)
                {
                    movement.SetDestination(lastPlayerPosition);
                }
                if (playerTarget != null) lastPlayerPosition = playerTarget.position;
                break;
            case BotState.Attack:
                if (playerTarget != null)
                {
                    float dist = Vector3.Distance(transform.position, playerTarget.position);
                    if (dist > settings.AttackRange)
                    {
                        // Masih terlalu jauh, mendekati player.
                        movement.SetDestination(playerTarget.position);
                    }
                    else
                    {
                        // Sudah dalam jarak serang: berhenti, hadap player, dan akan ditembak di bawah.
                        movement.Stop();
                        Vector3 toPlayer = playerTarget.position - transform.position;
                        toPlayer.y = 0f;
                        if (toPlayer.sqrMagnitude > 0.0001f)
                        {
                            transform.rotation = Quaternion.RotateTowards(
                                transform.rotation,
                                Quaternion.LookRotation(toPlayer, Vector3.up),
                                settings.RotationSpeed * Time.deltaTime
                            );
                        }
                    }
                }
                break;
            case BotState.Strafe:
                HandleStrafe();
                break;
            case BotState.Search:
                if (vision != null && vision.HasLastKnown)
                    movement.SetDestination(vision.LastKnownPosition);
                else
                    movement.Stop();
                break;
            case BotState.Flee:
                HandleFlee();
                break;
            case BotState.Recover:
                movement.Stop();
                break;
        }
    }

    private void HandlePatrol()
    {
        if (HasPatrolPoints)
        {
            if (patrolPoints[patrolIndex] != null)
                movement.SetDestination(patrolPoints[patrolIndex].position);
            // advance when close
            if (movement != null && Vector3.Distance(transform.position, patrolPoints[patrolIndex].position) < 1f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }
        }
        else
        {
            HandleWander();
        }
    }

    private void HandleWander()
    {
        if (!hasWanderTarget || wanderTimer <= 0f)
        {
            PickWanderTarget();
            wanderTimer = settings.WanderInterval;
        }
        wanderTimer -= Time.deltaTime;
        if (hasWanderTarget)
            movement.SetDestination(wanderTarget);
    }

    private void PickWanderTarget()
    {
        float r = settings.WanderRadius;
        Vector2 rnd = Random.insideUnitCircle * r;
        Vector3 basePos = transform.position;
        // wander around start? use current position
        wanderTarget = basePos + new Vector3(rnd.x, 0f, rnd.y);
        hasWanderTarget = true;
    }

    private void HandleStrafe()
    {
        if (playerTarget == null || movement == null) return;

        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDirection = -strafeDirection;
            strafeTimer = settings != null ? settings.StrafeChangeInterval : 2f;
        }

        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        Vector3 toPlayerDir = dist > 0.0001f ? toPlayer / dist : Vector3.forward;

        // Strafe direction: perpendicular to player (left/right).
        Vector3 strafeDir = Vector3.Cross(Vector3.up, toPlayerDir) * strafeDirection;

        // Keep distance near engageRange: move away if too close, move closer if too far.
        float engageRange = settings != null ? settings.EngageRange : 10f;
        Vector3 adjust = Vector3.zero;
        if (dist < engageRange * 0.6f)
            adjust -= toPlayerDir; // too close, back off
        else if (dist > engageRange)
            adjust += toPlayerDir; // too far, close in slightly

        Vector3 finalDir = (strafeDir + adjust);
        if (finalDir.sqrMagnitude < 0.0001f)
            finalDir = toPlayerDir;
        else
            finalDir.Normalize();

        movement.SetDestination(transform.position + finalDir * 8f);
    }

    private void HandleFlee()
    {
        if (playerTarget == null || movement == null) return;

        fleeTimer -= Time.deltaTime;
        if (fleeTimer <= 0f)
        {
            isFleeing = false;
            return;
        }

        // Run directly away from player.
        Vector3 away = transform.position - playerTarget.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -transform.forward;

        movement.SetDestination(transform.position + away.normalized * 10f);
    }

    private float GetCurrentSpeed()
    {
        if (settings == null || stateMachine == null) return 0f;
        switch (stateMachine.CurrentState)
        {
            case BotState.Chase:
            case BotState.Attack:
                return settings.ChaseSpeed;
            case BotState.Strafe:
                return settings.StrafeSpeed;
            case BotState.Flee:
                return settings.FleeSpeed;
            case BotState.Patrol:
                return settings.PatrolSpeed;
            case BotState.Investigate:
            case BotState.Search:
                return settings.WalkSpeed;
            case BotState.Idle:
                return settings.WalkSpeed * 0.4f;
            default:
                return 0f;
        }
    }

    private bool CanMoveInCurrentState()
    {
        if (stateMachine == null) return true;
        switch (stateMachine.CurrentState)
        {
            case BotState.Idle:
                return true;
            case BotState.Recover:
                return false;
            case BotState.Flee:
                return true;
            default:
                return true;
        }
    }

    private float RandomReactionDelay()
    {
        if (settings == null) return 0.1f;
        return Random.Range(settings.ReactionTimeMin, settings.ReactionTimeMax);
    }
}