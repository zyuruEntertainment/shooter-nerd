using UnityEngine;

public class BotDashController : MonoBehaviour
{
    [SerializeField] private BotSettings settings;

    private PlayerPhysics physics;
    private PlayerDash playerDash;
    private BotMovement movement;

    private float closeTimer;
    private bool wasClose;

    private void Awake()
    {
        if (settings == null) settings = GetComponent<BotSettings>();
        physics = GetComponent<PlayerPhysics>();
        playerDash = GetComponent<PlayerDash>();
        movement = GetComponent<BotMovement>();
    }

    // Call periodically while chasing/attacking.
    // If forceDash is true, dash immediately regardless of distance (used while fleeing).
    public void TryDash(Vector3 direction, bool allowDash, bool forceDash = false)
    {
        if (settings == null || physics == null || playerDash == null) return;
        if (!allowDash) { ResetClose(); return; }
        if (!physics.IsGrounded) { ResetClose(); return; }
        if (playerDash.IsDashing) { ResetClose(); return; }

        if (forceDash)
        {
            Vector3 dashDir = direction;
            dashDir.y = 0f;
            if (dashDir.sqrMagnitude < 0.0001f) dashDir = -transform.forward;
            dashDir.Normalize();

            float checkDist1 = 4f;
            Vector3 origin1 = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin1, dashDir, out RaycastHit hit1, checkDist1))
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj == null || hit1.transform == null || (hit1.transform != playerObj.transform && !hit1.transform.IsChildOf(playerObj.transform)))
                    return;
            }

            ResetClose();
            bool dashStarted = playerDash.Dash(dashDir);
            if (dashStarted) ResetClose();
            return;
        }

        float dist = direction.magnitude;
        bool close = dist <= settings.CloseRangeThreshold;

        if (close)
        {
            if (!wasClose)
            {
                closeTimer = 0f;
                wasClose = true;
            }
            closeTimer += Time.deltaTime;
        }
        else
        {
            ResetClose();
        }

        if (!close || closeTimer < settings.DashTriggerDuration) return;

        // Determine dash direction: away from player to create space.
        Vector3 away = -direction;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
        away.Normalize();

        // safety check: no obstacle along dash path
        float checkDist2 = 4f; // approximate; PlayerDash uses its own distance
        Vector3 origin2 = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin2, away, out RaycastHit hit2, checkDist2))
        {
            // if obstacle is not player, abort
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null || hit2.transform == null || (hit2.transform != p.transform && !hit2.transform.IsChildOf(p.transform)))
                return;
        }

        // also ensure not dashing already (handled). Execute.
        bool started = playerDash.Dash(away);
        if (started)
        {
            ResetClose();
        }
    }

    // PlayerDash manages its own cooldown internally; we just rely on Dash() returning false if on cooldown.

    private void ResetClose()
    {
        closeTimer = 0f;
        wasClose = false;
    }
}