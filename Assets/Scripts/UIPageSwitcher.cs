using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pengelola halaman UI: setiap halaman memiliki daftar RectTransform yang
/// ditampilkan dan disembunyikan. Dipakai untuk tombol onClick.
/// </summary>
[System.Serializable]
public class UIPage
{
    [Tooltip("Nama halaman (opsional, untuk identifikasi).")]
    public string name;

    [Tooltip("UI yang akan DITAMPILKAN saat halaman ini aktif. Halaman lain otomatis disembunyikan.")]
    public List<RectTransform> ditampilkan = new List<RectTransform>();
}

public class UIPageSwitcher : MonoBehaviour
{
    [Header("Daftar Halaman")]
    [Tooltip("List halaman. Setiap halaman punya daftar 'ditampilkan' dan 'disembunyikan'.")]
    [SerializeField] private List<UIPage> pages = new List<UIPage>();

    [Header("Halaman Awal")]
    [Tooltip("Indeks halaman yang ditampilkan saat Start. -1 = tidak ada.")]
    [SerializeField] private int defaultPage = 0;

    private int currentIndex = -1;

    private void Start()
    {
        // Hubungkan tombol Next/Previous otomatis jika belum di-wire di Inspector.
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn == null || btn.onClick.GetPersistentEventCount() > 0)
                continue;

            string lower = btn.gameObject.name.ToLowerInvariant();
            if (lower.Contains("next"))
                btn.onClick.AddListener(NextPage);
            else if (lower.Contains("previous") || lower.Contains("prev"))
                btn.onClick.AddListener(PreviousPage);
        }

        if (defaultPage >= 0 && defaultPage < pages.Count)
            ShowPage(defaultPage);
    }

    /// <summary>Menampilkan halaman berikutnya (next page).</summary>
    public void NextPage()
    {
        if (pages == null || pages.Count == 0)
            return;

        int next = currentIndex + 1;
        if (next >= pages.Count)
            next = 0; // wrap-around; ubah ke pages.Count - 1 jika tidak ingin loop
        ShowPage(next);
    }

    /// <summary>Menampilkan halaman sebelumnya (previous page).</summary>
    public void PreviousPage()
    {
        if (pages == null || pages.Count == 0)
            return;

        int prev = currentIndex - 1;
        if (prev < 0)
            prev = pages.Count - 1; // wrap-around; ubah ke 0 jika tidak ingin loop
        ShowPage(prev);
    }

    /// <summary>
    /// Fungsi untuk onClick: menampilkan UI pada halaman index dan
    /// menyembunyikan UI lainnya sesuai daftar.
    /// </summary>
    public void ShowPage(int index)
    {
        if (pages == null || index < 0 || index >= pages.Count)
            return;

        currentIndex = index;

        // Sembunyikan semua UI yang 'ditampilkan' dari halaman lain.
        for (int i = 0; i < pages.Count; i++)
        {
            if (i == index)
                continue;

            if (pages[i] == null || pages[i].ditampilkan == null)
                continue;

            foreach (RectTransform rt in pages[i].ditampilkan)
            {
                if (rt != null)
                    rt.gameObject.SetActive(false);
            }
        }

        UIPage page = pages[index];
        if (page == null)
            return;

        // Tampilkan UI pada halaman ini.
        if (page.ditampilkan != null)
        {
            foreach (RectTransform rt in page.ditampilkan)
            {
                if (rt != null)
                    rt.gameObject.SetActive(true);
            }
        }
    }
}