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

        BotState desired = CurrentState;

        if (visible)
        {
            desired = (dist <= s.AttackRange) ? BotState.Attack : BotState.Chase;
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
            desired = controller.HasPatrolPoints ? BotState.Patrol : BotState.Idle;
        }

        // keep investigate/search for their duration if no new info
        if (CurrentState == BotState.Investigate && stateTimer > 0f && !visible && status != PerceptionStatus.Heard)
            desired = BotState.Investigate;
        if (CurrentState == BotState.Search && stateTimer > 0f && !visible && status != PerceptionStatus.LastKnown)
            desired = BotState.Search;
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