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

    /// <summary>
    /// Reset health ke maksimal (dapat dipanggil meski dalam keadaan mati).
    /// Digunakan oleh RespawnManager saat objek respawning.
    /// </summary>
    public void ResetToMaxHealth()
    {
        CurrentHealth = maxHealth;
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} eliminated. Respawning...", this);

        // Ganti SetActive(false) dengan respawn sistem.
        // Objek akan dipindahkan ke titik respawn sesuai tag-nya dan health di-reset.
        RespawnManager.Instance?.Respawn(gameObject);
    }
}