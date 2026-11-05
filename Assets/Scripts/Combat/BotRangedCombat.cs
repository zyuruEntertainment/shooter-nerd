using UnityEngine;

[DisallowMultipleComponent]
public class BotRangedCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BotVision vision;
    [SerializeField] private Transform playerTarget;

    [Header("Game Balance")]
    [SerializeField, Min(1f)] private float attackRange = 20f;
    [SerializeField, Min(0f)] private float damage = 8f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1.1f;

    private CombatHealth playerHealth;
    private float nextAttackTime;

    private void Awake()
    {
        if (vision == null)
            vision = GetComponent<BotVision>();

        if (playerTarget == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerTarget = player.transform;
        }

        if (playerTarget != null)
            playerHealth = playerTarget.GetComponent<CombatHealth>();
    }

    private void Update()
    {
        if (vision == null ||
            playerTarget == null ||
            playerHealth == null)
            return;

        if (playerHealth.IsDead)
            return;

        if (!vision.CanSeePlayer)
            return;

        float distance = Vector3.Distance(
            transform.position,
            playerTarget.position
        );

        if (distance > attackRange)
            return;

        if (Time.time < nextAttackTime)
            return;

        Attack();
    }

    private void Attack()
    {
        nextAttackTime = Time.time + attackInterval;

        // Serangan game abstrak. Bot hanya menyerang
        // ketika target sudah terdeteksi oleh BotVision.
        playerHealth.TakeDamage(damage);
    }
}