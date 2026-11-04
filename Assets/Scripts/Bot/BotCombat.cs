using UnityEngine;

public class BotCombat : MonoBehaviour
{
    [SerializeField] private BotSettings settings;

    private float nextAttackTime;

    public float AttackRange => settings != null ? settings.AttackRange : 1.5f;

    private void Awake()
    {
        if (settings == null) settings = GetComponent<BotSettings>();
    }

    public bool TryAttack(Transform target)
    {
        if (settings == null || target == null) return false;
        if (Time.time < nextAttackTime) return false;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > settings.AttackRange) return false;

        // face target
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        nextAttackTime = Time.time + settings.AttackCooldown;

        // Optional damage hook: if target has a component implementing IDamageable, apply damage.
        var damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(1f);

        return true;
    }
}

// Optional interface for damageable targets. Implement on player/enemies if desired.
public interface IDamageable
{
    void TakeDamage(float amount);
}