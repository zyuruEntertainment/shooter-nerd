using UnityEngine;

public class BotCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private BotSettings settings;

    [Header("Animation")]
    [SerializeField, Min(0f)]
    private float attackAnimationDuration = 0.3f;

    [SerializeField, Min(0f)]
    private float attackDamage = 20f; // sejajar dengan damage pemain

    private float nextAttackTime;
    private float attackAnimationEndTime;

    public float AttackRange =>
        settings != null ? settings.AttackRange : 1.5f;

    public bool IsAttackAnimating =>
        Time.time < attackAnimationEndTime;

    private void Awake()
    {
        if (settings == null)
            settings = GetComponent<BotSettings>();
    }

    public bool TryAttack(Transform target)
    {
        if (settings == null || target == null)
            return false;

        if (Time.time < nextAttackTime)
            return false;

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance > settings.AttackRange)
            return false;

        // Face target.
        Vector3 direction =
            target.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(
                direction,
                Vector3.up
            );
        }

        // Start attack cooldown.
        nextAttackTime =
            Time.time + settings.AttackCooldown;

        // Trigger attack animation.
        attackAnimationEndTime =
            Time.time + attackAnimationDuration;

        // Apply damage if target supports IDamageable.
        IDamageable damageable =
    target.GetComponentInParent<IDamageable>();

        CombatHealth targetHealth =
            target.GetComponentInParent<CombatHealth>();

        CombatHealth ownHealth =
            GetComponent<CombatHealth>();

        float actualDamage = settings != null
            ? settings.AttackDamage
            : attackDamage;

        if (targetHealth != null)
        {
            targetHealth.TakeDamage(actualDamage, ownHealth);
        }
        else if (damageable != null)
        {
            damageable.TakeDamage(actualDamage);
        }

        return true;
    }
}

// Optional interface for damageable targets.
public interface IDamageable
{
    void TakeDamage(float amount);
}