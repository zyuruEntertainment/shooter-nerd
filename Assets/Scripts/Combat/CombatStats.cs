using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatStats : MonoBehaviour
{
    public int Kills { get; private set; }
    public int Deaths { get; private set; }

    public event Action OnStatsChanged;

    public void RegisterKill()
    {
        Kills++;
        OnStatsChanged?.Invoke();
    }

    public void RegisterDeath()
    {
        Deaths++;
        OnStatsChanged?.Invoke();
    }
}