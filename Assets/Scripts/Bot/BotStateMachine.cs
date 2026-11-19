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
        Debug.Log($"[BotStateMachine] {gameObject.name}: {CurrentState} -> {newState}");
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
            case BotState.Charge: stateTimer = controller.Settings.ChargeDuration; break;
            default: stateTimer = 0f; break;
        }
    }

    public void Evaluate()
    {
        if (controller == null) controller = GetComponent<BotController>();
        if (controller == null || controller.Settings == null) return;

        BotSettings s = controller.Settings;
        bool hasTarget = controller.HasValidCurrentTarget;
        PerceptionStatus status = controller.Perception != null ? controller.Perception.Status : PerceptionStatus.None;
        float dist = controller.DistanceToPlayer;
        bool visible = hasTarget && status == PerceptionStatus.Visible;
        bool canReact = controller.CanReact;
        bool damageInterrupt = controller.DamageInterruptPending;

        // handle recover timer
        if (controller.JustDashed)
        {
            controller.ClearJustDashed();
            SetState(BotState.Recover);
            return;
        }

        // Charge takes highest priority when bot just took damage.
        if (controller.IsCharging && hasTarget)
        {
            if (CurrentState != BotState.Charge)
                SetState(BotState.Charge);
            return;
        }

        BotState desired = CurrentState;

        // Tanpa target musuh yang valid, jangan masuk state bertempur.
        if (!hasTarget && (CurrentState == BotState.Chase || CurrentState == BotState.Attack ||
            CurrentState == BotState.Strafe || CurrentState == BotState.Charge))
        {
            desired = controller.HasPatrolPoints ? BotState.Patrol : BotState.Idle;
        }

        // If currently charging but timer expired, decide next state.
        if (CurrentState == BotState.Charge && stateTimer <= 0f)
        {
            // Setelah charge selesai: kalau sudah pernah ditembak player, langsung Chase (mengejar + tembak).
            // Kalau belum pernah ditembak, kembali ke Patrol/Idle.
            if (controller.HasBeenDamagedRecently)
            {
                float chargeTimeInState = Time.time - stateStartTime;
                bool chargeCanSwitch = chargeTimeInState >= s.MinStateDuration;
                if (chargeCanSwitch)
                    desired = BotState.Chase;
                return;
            }
            // fall through to normal evaluation below
        }

        if (visible)
        {
            // Jeda reaksi: bot tidak langsung mengetahui pemain terlihat.
            if (canReact)
            {
                if (dist <= s.EngageRange)
                    desired = BotState.Strafe; // lingkari + tembak, jangan berhenti diam
                else
                    desired = BotState.Chase; // mendekati pemain
            }
            // Jika belum bisa bereaksi, tetap pada state saat ini.
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
            // Tidak ada perception valid: jangan mengejar hanya karena pernah ditembak.
            desired = controller.HasPatrolPoints ? BotState.Patrol : BotState.Idle;
        }

        // keep investigate/search/flee for their duration if no new info
        if (CurrentState == BotState.Investigate && stateTimer > 0f && !visible && status != PerceptionStatus.Heard)
            desired = BotState.Investigate;
        if (CurrentState == BotState.Search && stateTimer > 0f && !visible && status != PerceptionStatus.LastKnown)
            desired = BotState.Search;
        if (CurrentState == BotState.Charge && stateTimer > 0f && !visible)
            desired = BotState.Charge;
        if (CurrentState == BotState.Recover && stateTimer > 0f && !visible)
            desired = BotState.Recover;

        if (desired == CurrentState) return;

        float timeInState = Time.time - stateStartTime;
        bool canSwitch = timeInState >= s.MinStateDuration;

        // Pemain terlihat dapat menginterupsi, tapi hanya jika reaksi sudah lewat.
        bool interrupt = visible && canReact;

        // Damage retaliation memiliki prioritas lebih tinggi.
        if (damageInterrupt)
        {
            interrupt = true;
            controller.ClearDamageInterrupt();
        }

        if (interrupt || canSwitch)
        {
            SetState(desired);
        }
    }
}