using UnityEngine;

[DisallowMultipleComponent]
public class BotRangedCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BotVision vision;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform shootOrigin; // kosongkan untuk menggunakan transform bot

    [Header("Game Balance")]
    [SerializeField, Min(1f)] private float attackRange = 20f;
    [SerializeField, Min(0f)] private float damage = 8f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1.1f;

    [Header("Aim Lock")]
    [SerializeField, Min(0f)] private float aimLockDelay = 0.5f; // jeda saat mengunci target sebelum tembak
    [SerializeField, Min(0f)] private float aimRotationSpeed = 720f;

    [Header("Debug Raycast")]
    [SerializeField] private bool showDebugRaycast = true;
    [SerializeField, Min(0f)] private float debugRaycastDuration = 0.05f;

    private CombatHealth playerHealth;
    private float nextAttackTime;
    private float aimTimer;
    private bool isAiming;

    private void Awake()
    {
        if (vision == null)
            vision = GetComponent<BotVision>();

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerTarget = player.transform;
        }

        if (playerTarget != null)
            playerHealth = playerTarget.GetComponent<CombatHealth>();

        if (shootOrigin == null)
            shootOrigin = transform;
    }

    private void Update()
    {
        if (vision == null ||
            playerTarget == null ||
            playerHealth == null ||
            shootOrigin == null)
            return;

        if (playerHealth.IsDead)
        {
            ResetAim();
            return;
        }

        if (!vision.CanSeePlayer)
        {
            ResetAim();
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance > attackRange)
        {
            ResetAim();
            return;
        }

        // ---- Kunci pandangan / aim ke player ----
        Vector3 toTarget = playerTarget.position - shootOrigin.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toTarget, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                aimRotationSpeed * Time.deltaTime
            );
        }

        // Debug ray saat mengunci arah (kuning selama delay, biru saat siap)
        if (showDebugRaycast)
        {
            Color aimColor = isAiming && aimTimer < aimLockDelay ? Color.yellow : Color.cyan;
            Debug.DrawRay(
                shootOrigin.position,
                toTarget.normalized * attackRange,
                aimColor,
                debugRaycastDuration
            );
        }

        // Jeda mengunci target sebelum boleh tembak
        if (!isAiming)
        {
            isAiming = true;
            aimTimer = 0f;
        }

        aimTimer += Time.deltaTime;

        if (aimTimer < aimLockDelay)
            return; // masih mengunci target, belum tembak

        // Jeda antar serangan
        if (Time.time < nextAttackTime)
            return;

        Fire();
    }

    private void Fire()
    {
        nextAttackTime = Time.time + attackInterval;
        ResetAim();

        Vector3 direction = playerTarget.position - shootOrigin.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(
            shootOrigin.position,
            direction.normalized,
            out hit,
            attackRange
        );

        // ---- Debug ray saat tembak (merah jika mengenai sesuatu, hijau jika meleset) ----
        if (showDebugRaycast)
        {
            Color hitColor = hitSomething ? Color.red : Color.green;
            Debug.DrawRay(
                shootOrigin.position,
                direction.normalized * attackRange,
                hitColor,
                debugRaycastDuration
            );
        }

        if (hitSomething)
        {
            // Terapkan damage hanya jika yang mengenai adalah player target
            CombatHealth health = hit.transform.GetComponentInParent<CombatHealth>();
            if (health != null && health == playerHealth)
                health.TakeDamage(damage);
        }
    }

    private void ResetAim()
    {
        isAiming = false;
        aimTimer = 0f;
    }
}
