using UnityEngine;

public class BotPerception : MonoBehaviour
{
    [SerializeField] private BotSettings settings;
    [SerializeField] private BotVision vision;
    [SerializeField] private BotHearing hearing;

    public PerceptionStatus Status { get; private set; } = PerceptionStatus.None;
    public Vector3 TargetPosition { get; private set; }
    public bool HasTarget { get; private set; }

    private void Awake()
    {
        if (settings == null) settings = GetComponent<BotSettings>();
        if (vision == null) vision = GetComponent<BotVision>();
        if (hearing == null) hearing = GetComponent<BotHearing>();
    }

    public void Refresh()
    {
        if (settings == null)
        {
            Status = PerceptionStatus.None;
            HasTarget = false;
            return;
        }

        float memory = settings.MemoryDuration;

        if (vision != null && vision.CanSeePlayer)
        {
            Status = PerceptionStatus.Visible;
            TargetPosition = vision.LastKnownPosition;
            HasTarget = true;
            return;
        }

        if (vision != null && vision.HasLastKnown)
        {
            Status = PerceptionStatus.LastKnown;
            TargetPosition = vision.LastKnownPosition;
            HasTarget = true;
            return;
        }

        if (hearing != null && hearing.HasRecentHearing(memory))
        {
            Status = PerceptionStatus.Heard;
            TargetPosition = hearing.LastHeardPosition;
            HasTarget = true;
            return;
        }

        Status = PerceptionStatus.None;
        HasTarget = false;
    }
}