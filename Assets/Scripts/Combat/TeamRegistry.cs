using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry sederhana untuk mencari target musuh berdasarkan tim.
/// </summary>
public static class TeamRegistry
{
    private static readonly List<CombatHealth> members = new List<CombatHealth>();

    public static void Register(CombatHealth health)
    {
        if (health == null) return;
        if (!members.Contains(health))
            members.Add(health);
    }

    public static void Unregister(CombatHealth health)
    {
        if (health == null) return;
        members.Remove(health);
    }

    /// <summary>Cari musuh terdekat yang tim-nya berbeda (dan bukan None).</summary>
    public static CombatHealth FindNearestEnemy(Vector3 position, Team selfTeam)
    {
        CombatHealth best = null;
        float bestSqr = float.MaxValue;

        for (int i = members.Count - 1; i >= 0; i--)
        {
            CombatHealth m = members[i];
            if (m == null)
            {
                members.RemoveAt(i);
                continue;
            }
            if (m.IsDead) continue;
            if (m.Team == Team.None || selfTeam == Team.None) continue;
            if (m.Team == selfTeam) continue;

            float sqr = (m.transform.position - position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = m;
            }
        }

        return best;
    }
}