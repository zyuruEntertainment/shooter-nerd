using UnityEngine;

[DisallowMultipleComponent]
public class BotRangedCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BotSettings settings;
    [SerializeField] private BotVision vision;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform shootOrigin; // kosongkan untuk menggunakan transform bot

    [Header("Game Balance")]
    [SerializeField, Min(1f)] private float attackRange = 20f;
    [SerializeField, Min(0f)] private float damage = 20f; // sejajar dengan damage pemain
    [SerializeField, Min(0.1f)] private float attackInterval = 1.1f; // jeda antar peluru (reload), satu-satunya delay

    [Header("Aim Lock")]
    [SerializeField, Min(0f)] private float aimLockDelay = 0f; // 0 = tidak ada jeda sebelum tembak, langsung pukul saat terlihat dan dalam range
    [SerializeField, Min(0f)] private float aimRotationSpeed = 540f; // kecepatan ikut aim; rendah = agak telat ikut player, tinggi = lebih responsif

    [Header("Target Leading")]
    [Tooltip("Seconds to lead the target (predict player movement). 0 = aim at current position (presisi maksimum).")]
    [SerializeField, Min(0f)] private float leadTime = 0f; // 0 = tembak ke posisi sekarang, presisi tajam tanpa prediksi

    [Header("Debug Raycast")]
    [SerializeField] private bool showDebugRaycast = true;
    [SerializeField, Min(0f)] private float debugRaycastDuration = 0.05f;

    private CombatHealth playerHealth;
    private PlayerPhysics playerPhysics;
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
        {
            playerHealth = playerTarget.GetComponent<CombatHealth>();
            playerPhysics = playerTarget.GetComponent<PlayerPhysics>();
        }

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

        // ---- Kunci pandangan / aim ke player (dengan leading/prediksi gerakan) ----
        Vector3 aimPos = GetPredictedTargetPosition();
        Vector3 toTarget = aimPos - shootOrigin.position;
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

        // Arah tembak menuju prediksi posisi pemain (lead), agar sulit dielak.
        Vector3 aimPos = GetPredictedTargetPosition();
        Vector3 direction = aimPos - shootOrigin.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        float actualDamage = settings != null ? settings.AttackDamage : damage;

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
                health.TakeDamage(actualDamage);
        }
    }

    // Posisi target yang diprediksi berdasarkan kecepatan pemain saat ini (leading).
    private Vector3 GetPredictedTargetPosition()
    {
        if (playerTarget == null) return shootOrigin.position;

        Vector3 pos = playerTarget.position;
        float lead = settings != null ? settings.LeadTime : leadTime;
        if (lead > 0f && playerPhysics != null)
        {
            pos += playerPhysics.HorizontalVelocity * lead;
        }
        pos.y = shootOrigin.position.y;
        return pos;
    }

    private void ResetAim()
    {
        isAiming = false;
        aimTimer = 0f;
    }
}
