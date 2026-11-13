using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistem respawn yang mengelompokkan titik respawn berdasarkan tag.
///
/// Inspector menyediakan dua field array:
///   - Player Respawn Points: digunakan saat objek dengan tag "Player" respawning.
///   - Enemy Respawn Points: digunakan saat objek dengan tag "Enemy" respawning.
///
/// Jika array kosong, RespawnManager akan mencari komponen RespawnPoint di scena
/// dan mengelompokkannya berdasarkan RespawnPoint.RespawnTag.
/// </summary>
[DisallowMultipleComponent]
public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Respawn Points (by Tag)")]
    [Tooltip("Drag titik respawn yang digunakan saat PLAYER respawning.")]
    [SerializeField] private Transform[] playerRespawnPoints = System.Array.Empty<Transform>();

    [Tooltip("Drag titik respawn yang digunakan saat ENEMY/bot respawning.")]
    [SerializeField] private Transform[] enemyRespawnPoints = System.Array.Empty<Transform>();

    [Header("Options")]
    [Tooltip("Jika true, otomatis mengumpulkan semua RespawnPoint di scena saat Awake.")]
    [SerializeField] private bool autoCollectFromScene = true;

    [Tooltip("Jeda detik sebelum respawn dijalankan (0 = langsung).")]
    [SerializeField, Min(0f)] private float respawnDelay = 0f;

    // Dictionary: tag -> daftar titik respawn
    private Dictionary<string, List<Transform>> pointsByTag = new Dictionary<string, List<Transform>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CollectFromScene();
    }

    private void CollectFromScene()
    {
        if (!autoCollectFromScene) return;

        var points = FindObjectsByType<RespawnPoint>();
        foreach (var point in points)
        {
            string tag = string.IsNullOrEmpty(point.RespawnTag) ? point.gameObject.tag : point.RespawnTag;
            AddPoint(tag, point.transform);
        }
    }

    private void AddPoint(string tag, Transform point)
    {
        if (point == null || string.IsNullOrEmpty(tag)) return;

        if (!pointsByTag.ContainsKey(tag))
            pointsByTag[tag] = new List<Transform>();

        if (!pointsByTag[tag].Contains(point))
            pointsByTag[tag].Add(point);
    }

    /// <summary>
    /// Respawning target di titik respawn acak yang cocok dengan tag-nya.
    /// Reset health melalui CombatHealth jika ada, dan mereset kecepatan fisika.
    /// </summary>
    public void Respawn(GameObject target)
    {
        if (target == null) return;

        if (respawnDelay > 0f)
        {
            StartCoroutine(RespawnAfterDelay(target, respawnDelay));
            return;
        }

        DoRespawn(target);
    }

    private System.Collections.IEnumerator RespawnAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        DoRespawn(target);
    }

    private void DoRespawn(GameObject target)
    {
        string tag = target.tag;
        List<Transform> points = GetPointsForTag(tag);

        if (points == null || points.Count == 0)
        {
            Debug.LogWarning($"[RespawnManager] Tidak ada titik respawn untuk tag '{tag}'. Objek tidak dipindah.", this);
            return;
        }

        Transform spawnPoint = points[Random.Range(0, points.Count)];

        // Pastikan objek aktif
        target.SetActive(true);

        // Reset health ke maksimal melalui CombatHealth
        CombatHealth health = target.GetComponent<CombatHealth>();
        if (health != null)
        {
            health.ResetToMaxHealth();
        }

        // Reset kecepatan fisika
        PlayerPhysics physics = target.GetComponent<PlayerPhysics>();
        if (physics != null)
        {
            physics.StopAllVelocity();
        }

        // Pindahkan posisi (gunakan CharacterController jika ada agar tidak bentrok collider)
        CharacterController controller = target.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            target.transform.position = spawnPoint.position;
            target.transform.rotation = spawnPoint.rotation;
            controller.enabled = true;
        }
        else
        {
            target.transform.position = spawnPoint.position;
            target.transform.rotation = spawnPoint.rotation;
        }
    }

    /// <summary>
    /// Mengembalikan daftar titik respawn untuk tag tertentu.
    /// </summary>
    public List<Transform> GetPointsForTag(string tag)
    {
        if (pointsByTag.TryGetValue(tag, out var list) && list.Count > 0)
            return list;

        // Fallback langsung ke field Inspector jika dictionary kosong
        if (tag == "Player" && playerRespawnPoints != null && playerRespawnPoints.Length > 0)
            return new List<Transform>(playerRespawnPoints);

        if (tag == "Enemy" && enemyRespawnPoints != null && enemyRespawnPoints.Length > 0)
            return new List<Transform>(enemyRespawnPoints);

        return null;
    }
}
