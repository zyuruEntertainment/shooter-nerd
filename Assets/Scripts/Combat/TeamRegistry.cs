using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Registry sederhana untuk mencari target musuh berdasarkan identitas tag.
/// </summary>
public static class TeamRegistry
{
    private static readonly List<CombatHealth> members = new List<CombatHealth>();

    static TeamRegistry()
    {
        // Bersihkan registry saat scene baru dimuat (mencegah stale references).
        SceneManager.sceneLoaded += (scene, mode) => members.Clear();
        SceneManager.sceneUnloaded += (scene) => members.Clear();
    }

    public static void Register(CombatHealth health)
    {
        if (health == null) return;
        if (!CombatHealth.IsValidIdentityTag(health.IdentityTag))
        {
            Debug.LogWarning($"[TeamRegistry] Registrasi objek dengan identitas tidak valid: {health.gameObject.name} (tag='{health.IdentityTag}').", health);
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

    /// <summary>Cari musuh terdekat dengan tag berbeda. Tidak pernah mengembalikan teammate.</summary>
    public static CombatHealth FindNearestEnemy(Vector3 position, string selfTag)
    {
        return FindNearestEnemy(position, selfTag, null);
    }

    public static CombatHealth FindNearestEnemy(Vector3 position, string selfTag, Transform self)
    {
        if (!CombatHealth.IsValidIdentityTag(selfTag))
        {
            Debug.LogWarning("[TeamRegistry] FindNearestEnemy dipanggil dengan identitas tidak valid. Tidak ada target valid.");
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

            // Identitas tidak valid tidak boleh menjadi target.
            if (!CombatHealth.IsValidIdentityTag(m.IdentityTag)) continue;

            // Jangan pernah kembalikan teammate (tag sama).
            if (m.IdentityTag == selfTag) continue;

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