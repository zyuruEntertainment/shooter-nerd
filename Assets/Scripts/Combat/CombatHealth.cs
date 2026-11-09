using UnityEngine;

public class CombatHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;
    private CombatHealth lastAttacker;

    public event System.Action<float> OnTakeDamage;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, CombatHealth attacker)
    {
        if (IsDead || amount <= 0f)
            return;

        if (attacker != null && attacker != this)
            lastAttacker = attacker;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        OnTakeDamage?.Invoke(amount);

        if (IsDead)
            Die();
    }

    public void RestoreHealth(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth = Mathf.Min(
            maxHealth,
            CurrentHealth + amount
        );
    }

    /// <summary>
    /// Reset health ke maksimal (dapat dipanggil meski dalam keadaan mati).
    /// Digunakan oleh RespawnManager saat objek respawning.
    /// </summary>
    public void ResetToMaxHealth()
    {
        CurrentHealth = maxHealth;
        lastAttacker = null;
    }

    private void Die()
    {
        // Tambahkan death untuk korban.
        GetComponent<CombatStats>()?.RegisterDeath();

        // Tambahkan kill untuk pembunuh terakhir.
        if (lastAttacker != null && lastAttacker != this)
        {
            lastAttacker
                .GetComponent<CombatStats>()
                ?.RegisterKill();
        }

        lastAttacker = null;

        Debug.Log($"{gameObject.name} eliminated.", this);

        RespawnManager.Instance?.Respawn(gameObject);
    }
}