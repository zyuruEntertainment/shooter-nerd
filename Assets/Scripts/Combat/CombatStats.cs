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

    public event Action OnStatsChanged;

    public void RegisterKill()
    {
        Kills += pointsPerKill;
        OnStatsChanged?.Invoke();
    }

    public void RegisterDeath()
    {
        Deaths += pointsPerDeath;
        OnStatsChanged?.Invoke();
    }
}