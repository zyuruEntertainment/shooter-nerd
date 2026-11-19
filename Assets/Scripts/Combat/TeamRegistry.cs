using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Registry sederhana untuk mencari target musuh berdasarkan tim.
/// </summary>
public static class TeamRegistry
{
    private static readonly List<CombatHealth> members = new List<CombatHealth>();
    private static bool sceneHooked = false;

    static TeamRegistry()
    {
        // Bersihkan registry saat scene baru dimuat (mencegah stale references).
        SceneManager.sceneLoaded += (scene, mode) => members.Clear();
        SceneManager.sceneUnloaded += (scene) => members.Clear();
        sceneHooked = true;
    }

    public static void Register(CombatHealth health)
    {
        if (health == null) return;
        if (health.Team == Team.None)
        {
            Debug.LogWarning($"[TeamRegistry] Registrasi objek dengan tim tidak valid (None): {health.gameObject.name}.", health);
        }
        if (!members.Contains(health))
            members.Add(health);
    }

    public static void Unregister(CombatHealth health)
    {
        if (health == null) return;
        members.Remove(health);
    }

    public static void Clear()
    {
        members.Clear();
    }

    /// <summary>Cari musuh terdekat yang tim-nya berbeda (dan valid). Tidak pernah mengembalikan teammate.</summary>
    public static CombatHealth FindNearestEnemy(Vector3 position, Team selfTeam)
    {
        return FindNearestEnemy(position, selfTeam, null);
    }

    public static CombatHealth FindNearestEnemy(Vector3 position, Team selfTeam, Transform self)
    {
        if (selfTeam == Team.None)
        {
            Debug.LogWarning("[TeamRegistry] FindNearestEnemy dipanggil dengan selfTeam=None. Tidak ada target valid.");
            return null;
        }

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

            // Objek yang sudah dihancurkan / tidak aktif / disabled tidak boleh jadi target.
            if (m.gameObject == null || !m.gameObject.activeInHierarchy || !m.enabled)
            {
                members.RemoveAt(i);
                continue;
            }

            if (m.IsDead) continue;

            // Tim tidak valid tidak boleh menjadi target (atau penyerang).
            if (m.Team == Team.None) continue;

            // Jangan pernah kembalikan teammate.
            if (m.Team == selfTeam) continue;

            // Jangan pernah kembalikan diri sendiri.
            if (self != null && (m.transform == self || m.transform.IsChildOf(self) || self.IsChildOf(m.transform)))
                continue;

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