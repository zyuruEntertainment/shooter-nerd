using UnityEngine;

/// <summary>
/// Marker component yang ditempatkan pada GameObject titik respawn.
/// RespawnManager mengumpulkan titik-titik ini dan mengelompokkannya berdasarkan RespawnTag.
/// Letakkan prefab/GameObject ini di scena, lalu beri tag "Player" atau "Enemy" (atau isi RespawnTag secara manual).
/// </summary>
[DisallowMultipleComponent]
public class RespawnPoint : MonoBehaviour
{
    [Header("Respawn Point")]
    [Tooltip("Tag yang digunakan untuk mengelompokkan titik ini (contoh: 'Player' atau 'Enemy').")]
    [SerializeField] private string respawnTag = "Player";

    public string RespawnTag => respawnTag;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
        Gizmos.color = Color.white;
    }
}
