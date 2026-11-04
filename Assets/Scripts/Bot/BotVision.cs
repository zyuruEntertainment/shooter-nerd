using UnityEngine;

public class BotVision : MonoBehaviour
{
    [SerializeField] private BotSettings settings;

    private Transform eye;
    private Transform target;
    private float visionTimer;
    private float lastSeenTime = -999f;
    private Vector3 lastSeenPosition;
    private bool hasLastSeen;

    public bool CanSeePlayer { get; private set; }
    public Vector3 LastKnownPosition => lastSeenPosition;
    public bool HasLastKnown => hasLastSeen && (Time.time - lastSeenTime) <= (settings != null ? settings.MemoryDuration : 3f);
    public float LastSeenTime => lastSeenTime;

    private void Awake()
    {
        if (settings == null) settings = GetComponent<BotSettings>();
        eye = transform; // use bot transform as eye; override via child if needed
        // try find a child named "Eye"
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            if (t.name.ToLower().Contains("eye"))
            {
                eye = t;
                break;
            }
        }
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    public void SetTarget(Transform t) { target = t; }

    public void SetEye(Transform t) { if (t != null) eye = t; }

    // Called periodically by BotController.
    public void CheckVision()
    {
        CanSeePlayer = false;
        if (settings == null || target == null || eye == null) return;

        Vector3 toTarget = target.position - eye.position;
        float sqrDist = toTarget.sqrMagnitude;
        float range = settings.VisionRange;
        if (sqrDist > range * range) return;

        Vector3 dir = toTarget.normalized;
        // field of view
        float halfFov = settings.FieldOfView * 0.5f;
        if (Vector3.Angle(eye.forward, dir) > halfFov) return;

        // raycast obstacle check
        float dist = Mathf.Sqrt(sqrDist);
        RaycastHit hit;
        if (Physics.Raycast(eye.position, dir, out hit, dist, settings.VisionObstacleMask))
        {
            // blocked if hit something that is not the player
            if (hit.transform != target && !hit.transform.IsChildOf(target))
                return;
        }

        // visible
        CanSeePlayer = true;
        lastSeenPosition = target.position;
        lastSeenTime = Time.time;
        hasLastSeen = true;
    }

    // Force a check respecting interval timer externally; helper.
    public bool ShouldCheck(float dt)
    {
        visionTimer -= dt;
        if (visionTimer <= 0f)
        {
            visionTimer = settings != null ? settings.VisionCheckInterval : 0.2f;
            return true;
        }
        return false;
    }
}