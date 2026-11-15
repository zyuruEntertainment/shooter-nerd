using TMPro;
using UnityEngine;

/// <summary>
/// Menampilkan hasil akhir match di scene Result: Kill, Death, dan Grade menggunakan TMP_Text UI.
///
/// CARA PAKAI (tidak otomatis membuat/mencari teks):
///   1. Di scene Result, buat GameObject teks sendiri (GameObject > UI > Text - TextMeshPro)
///      untuk Kill, Death, dan Grade.
///   2. Pasang script ini pada GameObject sembarang (contoh: "ResultUIManager").
///   3. Di Inspector, seret masing-masing GameObject teks ke field
///      Kill Text / Death Text / Grade Text.
///   4. Tekan Play. Teks akan diisi otomatis dari data hasil match yang disimpan MatchManager.
/// </summary>
[DisallowMultipleComponent]
public class ResultStatsUI : MonoBehaviour
{
    [Header("Teks UI (TMP) — buat GameObject teks sendiri di scene Result, lalu seret ke sini")]
    [Tooltip("Teks untuk menampilkan jumlah Kill.")]
    [SerializeField] private TMP_Text killText;

    [Tooltip("Teks untuk menampilkan jumlah Death.")]
    [SerializeField] private TMP_Text deathText;

    [Tooltip("Teks untuk menampilkan Grade (S, A, B, C, D, F).")]
    [SerializeField] private TMP_Text gradeText;

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        Refresh();

        if (killText == null)
            Debug.LogWarning("[ResultStatsUI] Kill Text belum di-assign di Inspector!");
        if (deathText == null)
            Debug.LogWarning("[ResultStatsUI] Death Text belum di-assign di Inspector!");
        if (gradeText == null)
            Debug.LogWarning("[ResultStatsUI] Grade Text belum di-assign di Inspector!");
    }

    /// <summary>Mengulang tampilan hasil dari data penyimpanan hasil match.</summary>
    public void Refresh()
    {
        if (!ResultData.HasResult)
        {
            SetText(killText, "0");
            SetText(deathText, "0");
            SetText(gradeText, "-");
            return;
        }

        SetText(killText, ResultData.Kills.ToString());
        SetText(deathText, ResultData.Deaths.ToString());
        SetText(gradeText, ResultData.Grade);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    /// <summary>
    /// Menghitung grade dari jumlah kill dan death (score = kill - death).
    /// S: >=10, A: >=7, B: >=5, C: >=3, D: >=1, F: 0 atau kurang.
    /// </summary>
    public static string HitungGrade(int kills, int deaths)
    {
        int score = kills - deaths;

        if (score >= 10) return "S";
        if (score >= 7) return "A";
        if (score >= 5) return "B";
        if (score >= 3) return "C";
        if (score >= 1) return "D";
        return "F";
    }
}
