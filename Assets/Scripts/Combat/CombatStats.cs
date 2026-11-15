using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatStats : MonoBehaviour
{
    [Header("Nilai Statistik")]
    [Tooltip("Nilai yang ditambahkan ke Kill setiap kali RegisterKill dipanggil (default 1).")]
    [SerializeField, Min(0)] private int pointsPerKill = 1;

    [Tooltip("Nilai yang ditambahkan ke Death setiap kali RegisterDeath dipanggil (default 1).")]
    [SerializeField, Min(0)] private int pointsPerDeath = 1;

    public int Kills { get; private set; }
    public int Deaths { get; private set; }

    // Akumulasi stat pemain lintas respawn/instance agar tidak hilang saat komponen berganti.
    public static int PlayerKills { get; private set; }
    public static int PlayerDeaths { get; private set; }

    public static void ResetPlayerStats()
    {
        PlayerKills = 0;
        PlayerDeaths = 0;
    }

    public event Action OnStatsChanged;

    public void RegisterKill()
    {
        Kills += pointsPerKill;
        if (IsPlayer())
            PlayerKills += pointsPerKill;
        OnStatsChanged?.Invoke();
    }

    public void RegisterDeath()
    {
        Deaths += pointsPerDeath;
        if (IsPlayer())
            PlayerDeaths += pointsPerDeath;
        OnStatsChanged?.Invoke();
    }

    private bool IsPlayer()
    {
        return string.Equals(gameObject.tag, "Player", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Mencari CombatStats pada GameObject yang sama, parent, atau child.
    /// </summary>
    public static CombatStats FindFor(CombatHealth health)
    {
        if (health == null) return null;
        CombatStats stats = health.GetComponent<CombatStats>();
        if (stats == null) stats = health.GetComponentInParent<CombatStats>();
        if (stats == null) stats = health.GetComponentInChildren<CombatStats>();
        return stats;
    }
}