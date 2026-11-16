using UnityEngine;

/// <summary>
/// Manager sederhana untuk menampilkan sebuah prefab UI di root hierarchy
/// (tanpa parent) sambil menyembunyikan UI lain.
/// </summary>
public class UIPrefabManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Prefab UI yang akan ditampilkan saat dipanggil.")]
    [SerializeField] private GameObject prefabUI;

    [Tooltip("UI yang akan disembunyikan saat prefab UI ditampilkan.")]
    [SerializeField] private GameObject ui;

    private GameObject spawnedInstance;

    /// <summary>
    /// Memanggil/menampilkan prefab UI di root hierarchy (tanpa parent)
    /// dan menyembunyikan UI yang ditentukan.
    /// </summary>
    public void Show()
    {
        if (prefabUI == null)
        {
            Debug.LogWarning("[UIPrefabManager] prefabUI belum diatur.");
            return;
        }

        if (spawnedInstance == null)
        {
            // Instantiate tanpa parent => langsung di root hierarchy.
            spawnedInstance = Instantiate(prefabUI);
        }

        spawnedInstance.SetActive(true);

        if (ui != null)
            ui.SetActive(false);
    }

    /// <summary>
    /// Menutup (menyembunyikan) prefab UI yang ditampilkan dan mengembalikan UI yang disembunyikan.
    /// Prefab tidak di-destroy, hanya SetActive(false), sehingga bisa dipanggil lagi.
    /// </summary>
    public void Close()
    {
        if (spawnedInstance != null)
        {
            // Sembunyikan UI dari prefab juga (bukan destroy).
            spawnedInstance.SetActive(false);
        }

        if (ui != null)
            ui.SetActive(true);
    }
}
