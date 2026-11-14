using System;
using UnityEngine;

/// <summary>
/// Antarmuka untuk objek yang dapat menerima damage.
/// Digunakan oleh CombatHealth dan sistem serangan (melee/ranged).
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}

public class CombatHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    [Header("Team")]
    [Tooltip("Tim objek ini. None = tidak tergabung tim (damage normal).")]
    [SerializeField] private Team team = Team.None;

    public Team Team => team;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;
    private CombatHealth lastAttacker;

    public event System.Action<float> OnTakeDamage;

    [Header("Kill Reward")]
    [Tooltip("Jumlah kredit minimum yang diberikan kepada pembunuh saat kill.")]
    [SerializeField, Min(0)] private int minKillReward = 250;
    [Tooltip("Jumlah kredit maksimum yang diberikan kepada pembunuh saat kill.")]
    [SerializeField, Min(0)] private int maxKillReward = 500;

    private void Awake()
    {
        // Default tim berdasarkan tag agar tidak perlu edit prefab/scene.
        if (team == Team.None)
        {
            string t = tag.ToLowerInvariant();
            if (t == "player" || t == "ally" || t == "teama")
                team = Team.TeamA;
            else if (t == "enemy" || t == "teamb")
                team = Team.TeamB;
        }

        CurrentHealth = maxHealth;
        TeamRegistry.Register(this);
    }

    private void OnDestroy()
    {
        TeamRegistry.Unregister(this);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, CombatHealth attacker)
    {
        if (IsDead || amount <= 0f)
            return;

        // Cegah friendly fire: tim yang sama tidak saling damage.
        if (attacker != null && attacker != this &&
            attacker.Team != Team.None && team != Team.None &&
            attacker.Team == team)
        {
            return;
        }

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

        // Berikan reward kredit kepada pembunuh (lastAttacker).
        if (lastAttacker != null && lastAttacker != this)
        {
            // Tambahkan kill statistik.
            lastAttacker.GetComponent<CombatStats>()?.RegisterKill();

            // Hitung reward random antara min dan max.
            int reward = UnityEngine.Random.Range(minKillReward, maxKillReward + 1);

            // Berikan kredit sesuai jenis pembunuh (PlayerWallet atau BotBuyWeapon).
            var playerWallet = lastAttacker.GetComponent<PlayerWallet>();
            if (playerWallet != null)
            {
                playerWallet.AddCredits(reward);
                Debug.Log($"[KillReward] Pemain mendapatkan +{reward} credits dari kill {gameObject.name}");
            }

            var botBuyWeapon = lastAttacker.GetComponent<BotBuyWeapon>();
            if (botBuyWeapon != null)
            {
                botBuyWeapon.AddCredits(reward);
                Debug.Log($"[KillReward] Bot mendapatkan +{reward} credits dari kill {gameObject.name}");
            }

            // Tampilkan teks kill reward via KillRewardManager.
            if (KillRewardManager.Instance != null)
            {
                KillRewardManager.Instance.ShowReward(reward, transform.position);
            }
            else
            {
                Debug.LogWarning("[CombatHealth] KillRewardManager.Instance belum ada di scene!");
            }
        }

        lastAttacker = null;

        Debug.Log($"{gameObject.name} eliminated.", this);

        RespawnManager.Instance?.Respawn(gameObject);
    }
}
