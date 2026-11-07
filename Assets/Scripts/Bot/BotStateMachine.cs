using UnityEngine;

public class BotStateMachine : MonoBehaviour
{
    [SerializeField] private BotController controller;

    public BotState CurrentState { get; private set; } = BotState.Idle;
    private float stateStartTime;
    private float stateTimer;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<BotController>();
        stateStartTime = Time.time;
    }

    private void Update()
    {
        if (stateTimer > 0f) stateTimer -= Time.deltaTime;
    }

    public void SetState(BotState newState)
    {
        if (newState == CurrentState) return;
        CurrentState = newState;
        stateStartTime = Time.time;
        ApplyStateTimer(newState);
    }

    private void ApplyStateTimer(BotState s)
    {
        if (controller == null || controller.Settings == null) return;
        switch (s)
        {
            case BotState.Investigate: stateTimer = controller.Settings.InvestigateDuration; break;
            case BotState.Search: stateTimer = controller.Settings.SearchDuration; break;
            case BotState.Recover: stateTimer = controller.Settings.RecoverDuration; break;
            case BotState.Flee: stateTimer = controller.Settings.FleeDuration; break;
            default: stateTimer = 0f; break;
        }
    }

    public void Evaluate()
    {
        if (controller == null) controller = GetComponent<BotController>();
        if (controller == null || controller.Settings == null) return;

        BotSettings s = controller.Settings;
        PerceptionStatus status = controller.Perception != null ? controller.Perception.Status : PerceptionStatus.None;
        float dist = controller.DistanceToPlayer;
        bool visible = status == PerceptionStatus.Visible;

        // handle recover timer
        if (controller.JustDashed)
        {
            controller.ClearJustDashed();
            SetState(BotState.Recover);
            return;
        }

        // Flee takes highest priority when bot just took damage.
        if (controller.IsFleeing)
        {
            if (CurrentState != BotState.Flee)
                SetState(BotState.Flee);
            return;
        }

        BotState desired = CurrentState;

        // If currently fleeing but timer expired, decide next state.
        if (CurrentState == BotState.Flee && stateTimer <= 0f)
        {
            // Setelah kabur selesai: kalau sudah pernah ditembak player, langsung Chase (mengejar + tembak).
            // Kalau belum pernah ditembak, kembali ke Patrol/Idle.
            if (controller.HasBeenDamagedRecently)
            {
                float fleeTimeInState = Time.time - stateStartTime;
                bool fleeCanSwitch = fleeTimeInState >= s.MinStateDuration;
                if (fleeCanSwitch)
                    desired = BotState.Chase;
                return;
            }
            // fall through to normal evaluation below
        }

        if (visible)
        {
            if (dist <= s.AttackRange)
                desired = BotState.Attack;
            else if (dist <= s.EngageRange)
                desired = BotState.Strafe;
            else
                desired = BotState.Chase;
        }
        else if (status == PerceptionStatus.LastKnown)
        {
            desired = BotState.Search;
        }
        else if (status == PerceptionStatus.Heard)
        {
            desired = BotState.Investigate;
        }
        else
        {
            // Kondisi 1: tidak pernah ditembak player → Patrol / Idle.
            // Kondisi 2: sudah pernah ditembak player → Chase (mengejar dan menembak).
            if (controller.HasBeenDamagedRecently)
                desired = BotState.Chase;
            else
                desired = controller.HasPatrolPoints ? BotState.Patrol : BotState.Idle;
        }

        // keep investigate/search/flee for their duration if no new info
        if (CurrentState == BotState.Investigate && stateTimer > 0f && !visible && status != PerceptionStatus.Heard)
            desired = BotState.Investigate;
        if (CurrentState == BotState.Search && stateTimer > 0f && !visible && status != PerceptionStatus.LastKnown)
            desired = BotState.Search;
        if (CurrentState == BotState.Flee && stateTimer > 0f && !visible)
            desired = BotState.Flee;
        if (CurrentState == BotState.Recover && stateTimer > 0f && !visible)
            desired = BotState.Recover;

        if (desired == CurrentState) return;

        float timeInState = Time.time - stateStartTime;
        bool canSwitch = timeInState >= s.MinStateDuration;

        // Visible player can interrupt immediately.
        bool interrupt = visible;

        if (interrupt || canSwitch)
        {
            SetState(desired);
        }
    }
}