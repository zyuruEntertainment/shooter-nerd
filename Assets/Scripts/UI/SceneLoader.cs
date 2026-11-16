using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Helper untuk berpindah scene yang selalu melewati scene "Loading" terlebih dahulu,
/// meniru alur yang dipakai MatchManager (LoadingScreen.Load).
/// Dapat dipakai dari tombol UI (OnClick) maupun dipanggil dari kode lain.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Nama scene tujuan yang akan dimuat setelah scene Loading selesai.")]
    [SerializeField] private string targetSceneName = "Gameplay";

    /// <summary>
    /// Memuat scene tujuan melalui scene Loading (statis, bisa dipanggil dari mana saja).
    /// Meniru MatchManager: pastikan pause/shop normal dan timeScale = 1 sebelum transisi.
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoader] Nama scene tujuan kosong, pembatalan pindah scene.");
            return;
        }

        // Pastikan semua sistem pause/shop sudah dalam kondisi normal sebelum pindah scene.
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            Debug.Log("[SceneLoader] PauseManager sedang pause, memanggil Resume() sebelum pindah scene.");
            PauseManager.Instance.Resume();
        }

        if (GameplayUIManager.Instance != null && GameplayUIManager.Instance.IsShopOpen)
        {
            Debug.Log("[SceneLoader] GameplayUIManager shop terbuka, memanggil CloseShop() sebelum pindah scene.");
            GameplayUIManager.Instance.CloseShop();
        }

        // Pastikan waktu tidak dalam kondisi freeze saat pindah scene.
        Time.timeScale = 1f;
        Debug.Log($"[SceneLoader] Memuat scene '{sceneName}' via Loading.");

        // Melewati scene Loading terlebih dahulu, baru ke scene tujuan.
        LoadingScreen.Load(sceneName);
    }

    /// <summary>
    /// Memuat scene yang diatur pada field targetSceneName (untuk tombol UI).
    /// </summary>
    public void LoadTargetScene()
    {
        LoadScene(targetSceneName);
    }

    /// <summary>
    /// Alias yang nyaman untuk dihubungkan ke Button.OnClick di Inspector.
    /// </summary>
    public void OnChangeSceneClicked()
    {
        LoadTargetScene();
    }

    /// <summary>
    /// Overload untuk Button.OnClick yang menerima nama scene langsung dari Inspector.
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        LoadScene(sceneName);
    }
}
