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
    [Tooltip("Konfigurasi tim serialized (fallback untuk tag yang tidak dikenal).")]
    [SerializeField] private Team team = Team.None;

    // Nilai resolved saat runtime (tidak serialized).
    private Team resolvedTeam = Team.None;
    private bool identityResolved = false;

    public Team Team => identityResolved ? resolvedTeam : team;
    public Team ResolvedTeam => Team;

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
        ResolveTeamIdentity();

        CurrentHealth = maxHealth;
        TeamRegistry.Register(this);
    }

    private void ResolveTeamIdentity()
    {
        Team serialized = team;
        Team recognized = ResolveRecognizedTeam();

        if (TeamResolver.IsRecognizedTag(gameObject.tag) || recognized != Team.None)
        {
            // Tag dikenal: override nilai serialized yang konflik.
            if (serialized != Team.None && serialized != recognized)
            {
                Debug.LogWarning($"[CombatHealth] {gameObject.name}: tag '{gameObject.tag}' maps to {recognized}, but serialized team is {serialized}. Using tag value.", this);
            }
            resolvedTeam = recognized;
        }
        else
        {
            // Tag tidak dikenal: fallback ke konfigurasi serialized.
            // Dukung CombatHealth pada child dengan tag pada root.
            Team inherited = ResolveRecognizedTeamFromAncestors();
            if (inherited != Team.None)
            {
                if (serialized != Team.None && serialized != inherited)
                {
                    Debug.LogWarning($"[CombatHealth] {gameObject.name}: ancestor tag maps to {inherited}, but serialized team is {serialized}. Using ancestor tag value.", this);
                }
                resolvedTeam = inherited;
            }
            else
            {
                resolvedTeam = serialized;
                if (serialized == Team.None)
                {
                    Debug.LogWarning($"[CombatHealth] {gameObject.name} tidak memiliki tim valid (tag='{gameObject.tag}'). Friendly fire tidak akan diterapkan secara diam-diam.", this);
                }
                else
                {
                    Debug.Log($"[CombatHealth] {gameObject.name} tim = {resolvedTeam} (serialized fallback).", this);
                }
            }
        }

        if (resolvedTeam != Team.None && (TeamResolver.IsRecognizedTag(gameObject.tag) || recognized != Team.None))
        {
            Debug.Log($"[CombatHealth] {gameObject.name} tim = {resolvedTeam} (tag '{gameObject.tag}').", this);
        }

        identityResolved = true;

        if (resolvedTeam == Team.None)
        {
            Debug.LogWarning($"[CombatHealth] {gameObject.name} resolved team = None.", this);
        }
    }

    private Team ResolveRecognizedTeam()
    {
        // Periksa tag GameObject ini sendiri.
        Team t = TeamResolver.FromTag(gameObject.tag);
        if (t != Team.None) return t;
        return Team.None;
    }

    private Team ResolveRecognizedTeamFromAncestors()
    {
        Transform t = transform.parent;
        while (t != null)
        {
            // Hindari inheritance lintas entity: berhenti jika bertemu CombatHealth lain.
            var other = t.GetComponent<CombatHealth>();
            if (other != null && other != this)
                break;

            Team mapped = TeamResolver.FromTag(t.gameObject.tag);
            if (mapped != Team.None)
                return mapped;

            t = t.parent;
        }
        return Team.None;
    }

    private void OnEnable()
    {
        if (identityResolved)
            TeamRegistry.Register(this);
    }

    private void OnDisable()
    {
        if (identityResolved)
            TeamRegistry.Unregister(this);
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

        // Validasi tim sebelum menerapkan damage.
        if (attacker != null && attacker != this)
        {
            Team myTeam = Team;
            // Tim yang sama: cegah friendly fire.
            if (attacker.Team != Team.None && myTeam != Team.None && attacker.Team == myTeam)
            {
                Debug.LogWarning($"[CombatHealth] Friendly fire diblokir: {attacker.gameObject.name} ({attacker.Team}) -> {gameObject.name} ({myTeam}).", this);
                return;
            }

            // Tim tidak valid (None): perilaku eksplisit = tolak damage (tidak diam-diam mengizinkan).
            if (attacker.Team == Team.None || myTeam == Team.None)
            {
                Debug.LogWarning($"[CombatHealth] Damage diblokir karena tim tidak valid: attacker={attacker.gameObject.name} ({attacker.Team}), victim={gameObject.name} ({myTeam}).", this);
                return;
            }
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
        CombatStats victimStats = CombatStats.FindFor(this);
        if (victimStats != null) victimStats.RegisterDeath();

        // Berikan reward kredit kepada pembunuh (lastAttacker).
        if (lastAttacker != null && lastAttacker != this)
        {
            // Tambahkan kill statistik.
            CombatStats attackerStats = CombatStats.FindFor(lastAttacker);
            if (attackerStats != null) attackerStats.RegisterKill();

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

            // Tambah poin tim pembunuh.
            if (PointManager.Instance != null)
                PointManager.Instance.AddKill(lastAttacker.Team);

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
