using UnityEngine;

[DisallowMultipleComponent]
public class BotRangedCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BotSettings settings;
    [SerializeField] private BotVision vision;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform shootOrigin;

    [Header("Game Balance")]
    [SerializeField, Min(1f)] private float attackRange = 20f;
    [SerializeField, Min(0f)] private float damage = 20f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1.1f;

    [Header("Aim Lock")]
    [SerializeField, Min(0f)] private float aimLockDelay = 0f;
    [SerializeField, Min(0f)] private float aimRotationSpeed = 540f;

    [Header("Target Leading")]
    [Tooltip("Seconds to lead the target. 0 = aim at current position.")]
    [SerializeField, Min(0f)] private float leadTime = 0f;

    [Header("Debug Raycast")]
    [SerializeField] private bool showDebugRaycast = true;
    [SerializeField, Min(0f)] private float debugRaycastDuration = 0.05f;

    private CombatHealth playerHealth;
    private CombatHealth ownHealth;

    private PlayerPhysics playerPhysics;

    private float nextAttackTime;
    private float aimTimer;
    private bool isAiming;

    private void Awake()
    {
        if (settings == null)
            settings = GetComponent<BotSettings>();

        if (vision == null)
            vision = GetComponent<BotVision>();

        ownHealth = GetComponent<CombatHealth>();

        if (playerTarget == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerTarget = player.transform;
        }

        if (playerTarget != null)
        {
            playerHealth =
                playerTarget.GetComponent<CombatHealth>();

            playerPhysics =
                playerTarget.GetComponent<PlayerPhysics>();
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

        if (playerHealth.IsDead ||
            (ownHealth != null && ownHealth.IsDead))
        {
            ResetAim();
            return;
        }

        if (!vision.CanSeePlayer)
        {
            ResetAim();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            playerTarget.position
        );

        if (distance > attackRange)
        {
            ResetAim();
            return;
        }

        // Aim menuju posisi player.
        Vector3 aimPos = GetPredictedTargetPosition();

        Vector3 toTarget =
            aimPos - shootOrigin.position;

        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot =
                Quaternion.LookRotation(toTarget, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                aimRotationSpeed * Time.deltaTime
            );
        }

        // Debug aim ray.
        if (showDebugRaycast)
        {
            Color aimColor =
                isAiming && aimTimer < aimLockDelay
                ? Color.yellow
                : Color.cyan;

            Debug.DrawRay(
                shootOrigin.position,
                toTarget.normalized * attackRange,
                aimColor,
                debugRaycastDuration
            );
        }

        // Aim lock.
        if (!isAiming)
        {
            isAiming = true;
            aimTimer = 0f;
        }

        aimTimer += Time.deltaTime;

        if (aimTimer < aimLockDelay)
            return;

        // Attack cooldown.
        if (Time.time < nextAttackTime)
            return;

        Fire();
    }

    private void Fire()
    {
        nextAttackTime = Time.time + attackInterval;

        ResetAim();

        Vector3 aimPos = GetPredictedTargetPosition();

        Vector3 direction =
            aimPos - shootOrigin.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        float actualDamage =
            settings != null
            ? settings.AttackDamage
            : damage;

        bool hitSomething = Physics.Raycast(
            shootOrigin.position,
            direction.normalized,
            out RaycastHit hit,
            attackRange
        );

        // Debug shooting ray.
        if (showDebugRaycast)
        {
            Color hitColor =
                hitSomething ? Color.red : Color.green;

            Debug.DrawRay(
                shootOrigin.position,
                direction.normalized * attackRange,
                hitColor,
                debugRaycastDuration
            );
        }

        if (!hitSomething)
            return;

        // Cari health dari collider yang terkena.
        CombatHealth health =
            hit.collider.GetComponentInParent<CombatHealth>();

        if (health == null || health != playerHealth)
            return;

        // Damage membawa identitas bot sebagai attacker.
        health.TakeDamage(actualDamage, ownHealth);
    }

    private Vector3 GetPredictedTargetPosition()
    {
        if (playerTarget == null)
            return shootOrigin.position;

        Vector3 pos = playerTarget.position;

        float lead =
            settings != null
            ? settings.LeadTime
            : leadTime;

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