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
    private BotJumpController jumpController;
    private BotDashController dashController;
    private BotStateMachine stateMachine;

    private CharacterController controller;
    private PlayerPhysics physics;

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

    public BotSettings Settings => settings;
    public BotPerception Perception => perception;
    public BotStateMachine StateMachine => stateMachine;
    public bool HasPatrolPoints => patrolPoints != null && patrolPoints.Length > 0;
    public bool JustDashed => justDashed;
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
        jumpController = GetComponent<BotJumpController>();
        dashController = GetComponent<BotDashController>();
        stateMachine = GetComponent<BotStateMachine>();

        controller = GetComponent<CharacterController>();
        physics = GetComponent<PlayerPhysics>();

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

        visionTimer = settings != null ? settings.VisionCheckInterval : 0.2f;
        stateEvalTimer = settings != null ? settings.StateEvaluationInterval : 0.15f;
        reactionTimer = RandomReactionDelay();
        wanderTimer = 0f;

        justDashed = false;
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
            movement.Tick(dt, speed, canMove);
        }

        // Jump / dash attempts
        BotState st = stateMachine != null ? stateMachine.CurrentState : BotState.Idle;
        bool chasing = st == BotState.Chase || st == BotState.Attack;
        if (chasing && playerTarget != null)
        {
            float dist = Vector3.Distance(transform.position, playerTarget.position);
            Vector3 toPlayer = playerTarget.position - transform.position;

            if (jumpController != null)
            {
                bool clearance = jumpController.HasForwardClearance(transform.forward, 1.5f);
                jumpController.TryJump(true, dist, clearance);
            }

            if (dashController != null)
            {
                dashController.TryDash(toPlayer, true);
                // detect if dash started this frame
                var pd = GetComponent<PlayerDash>();
                // justDashed set when dash begins; we approximate by checking IsDashing rising edge
            }
        }

        // Track dash rising edge
        TrackDashRisingEdge();

        // Combat
        if (st == BotState.Attack && combat != null && playerTarget != null)
        {
            combat.TryAttack(playerTarget);
        }

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
                    movement.SetDestination(playerTarget.position);
                break;
            case BotState.Search:
                if (vision != null && vision.HasLastKnown)
                    movement.SetDestination(vision.LastKnownPosition);
                else
                    movement.Stop();
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

    private float GetCurrentSpeed()
    {
        if (settings == null || stateMachine == null) return 0f;
        switch (stateMachine.CurrentState)
        {
            case BotState.Chase:
            case BotState.Attack:
                return settings.ChaseSpeed;
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