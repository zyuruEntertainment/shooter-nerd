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
    public void TryDash(Vector3 toPlayer, bool allowDash)
    {
        if (settings == null || physics == null || playerDash == null) return;
        if (!allowDash) { ResetClose(); return; }
        if (!physics.IsGrounded) { ResetClose(); return; }
        if (playerDash.IsDashing) { ResetClose(); return; }

        float dist = toPlayer.magnitude;
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
        Vector3 away = -toPlayer;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
        away.Normalize();

        // safety check: no obstacle along dash path
        float checkDist = 4f; // approximate; PlayerDash uses its own distance
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, away, out RaycastHit hit, checkDist))
        {
            // if obstacle is not player, abort
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null || hit.transform == null || (hit.transform != p.transform && !hit.transform.IsChildOf(p.transform)))
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