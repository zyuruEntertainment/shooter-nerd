using UnityEngine;

public class CombatHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;

    public event System.Action<float> OnTakeDamage;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

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

    private void Die()
    {
        Debug.Log($"{gameObject.name} eliminated.", this);

        // Bisa diganti dengan sistem respawn atau death animation.
        gameObject.SetActive(false);
    }
}