using UnityEngine;

public class BotJumpController : MonoBehaviour
{
    [SerializeField] private BotSettings settings;

    private PlayerPhysics physics;
    private PlayerJump playerJump;
    private float nextJumpTime;

    private void Awake()
    {
        if (settings == null) settings = GetComponent<BotSettings>();
        physics = GetComponent<PlayerPhysics>();
        playerJump = GetComponent<PlayerJump>();
    }

    // Call periodically. Conditions decide whether to jump.
    public void TryJump(bool isChasing, float distanceToPlayer, bool hasPathClearance)
    {
        if (settings == null || physics == null || playerJump == null) return;
        if (!physics.IsGrounded) return;
        if (Time.time < nextJumpTime) return;
        if (!isChasing) return;
        if (distanceToPlayer > settings.JumpTriggerDistance) return;
        if (!hasPathClearance) return;

        // small chance
        if (Random.value > settings.JumpProbability) 
        {
            // still set a short cooldown to avoid spamming checks
            nextJumpTime = Time.time + 0.3f;
            return;
        }

        playerJump.Jump();
        nextJumpTime = Time.time + settings.JumpCooldown;
    }

    // Lompat sesekali saat mengejar, tidak dibatasi oleh jarak ke player.
    public void TryChaseJump()
    {
        if (settings == null || physics == null || playerJump == null) return;
        if (!physics.IsGrounded) return;
        if (Time.time < nextJumpTime) return;

        if (Random.value > settings.JumpProbability)
        {
            nextJumpTime = Time.time + 0.3f;
            return;
        }

        playerJump.Jump();
        nextJumpTime = Time.time + settings.JumpCooldown;
    }

    public bool HasForwardClearance(Vector3 forward, float distance)
    {
        if (physics == null) return true;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;
        if (Physics.Raycast(origin, forward, out hit, distance))
        {
            // allow if hit is the player
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null && hit.transform != null && (hit.transform == p.transform || hit.transform.IsChildOf(p.transform)))
                return true;
            return false;
        }
        return true;
    }
}