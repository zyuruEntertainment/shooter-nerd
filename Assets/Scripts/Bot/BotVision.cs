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
    public Transform TargetTransform => target;
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
        // Target ditentukan oleh BotController (single source of truth).
    }

    public void SetTarget(Transform t)
    {
        if (t == target) return;
        target = t;
        // Reset stale visibility state saat target berubah.
        CanSeePlayer = false;
        hasLastSeen = false;
        lastSeenTime = -999f;
    }

    public void SetLastKnownPosition(Vector3 pos)
    {
        lastSeenPosition = pos;
        lastSeenTime = Time.time;
        hasLastSeen = true;
    }

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

        // raycast obstacle check: ignore bot's own colliders; target colliders (incl. child) do not block.
        float dist = Mathf.Sqrt(sqrDist);
        RaycastHit[] hits = Physics.RaycastAll(eye.position, dir, dist, settings.VisionObstacleMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            Transform ht = h.transform;

            // Abaikan collider milik bot sendiri.
            if (ht == transform || ht.IsChildOf(transform) || transform.IsChildOf(ht))
                continue;

            // Jika mengenai target atau child-nya, terlihat.
            if (ht == target || ht.IsChildOf(target) || target.IsChildOf(ht))
            {
                CanSeePlayer = true;
                lastSeenPosition = target.position;
                lastSeenTime = Time.time;
                hasLastSeen = true;
                return;
            }

            // Obstacle lain memblokir.
            return;
        }

        // Tidak ada hit yang memblokir.
        CanSeePlayer = true;
        lastSeenPosition = target.position;
        lastSeenTime = Time.time;
        hasLastSeen = true;
    }

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