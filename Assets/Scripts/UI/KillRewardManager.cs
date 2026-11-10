using TMPro;
using UnityEngine;

/// <summary>
/// Manajer kill reward yang menampilkan teks "+250" di layar saat mendapatkan kill.
/// Pasang pada GameObject kosong di scene. Tidak menampilkan apa-apa di awal.
/// </summary>
public class KillRewardManager : MonoBehaviour
{
    public static KillRewardManager Instance { get; private set; }

    [Header("Referensi Teks UI")]
    [SerializeField] private TextMeshProUGUI rewardText; // TMP_Text UI yang di-assign di Inspector

    [Header("Pengaturan Animasi")]
    [SerializeField] private float floatSpeed = 50f; // kecepatan melayang ke atas (piksel per detik)
    [SerializeField] private float fadeDuration = 1f; // durasi memudah
    [SerializeField] private float displayDuration = 2f; // total waktu tampil

    private float timer;
    private bool isActive;
    private Vector3 startPosition;
    private Color startColor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Sembunyikan teks di awal
        if (rewardText != null)
        {
            rewardText.text = string.Empty;
            rewardText.enabled = false;
        }

        Debug.Log("[KillRewardManager] Siap. Teks disembunyikan di awal.");
    }

    /// <summary>Tampilkan teks kill reward di layar. Dipanggil saat mendapatkan kill.</summary>
    public void ShowReward(int rewardAmount, Vector3 worldPosition)
    {
        if (rewardText == null)
        {
            Debug.LogWarning("[KillRewardManager] rewardText belum di-assign di Inspector!");
            return;
        }

        // Konversi posisi dunia ke layar
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        screenPos.z = 0f;

        rewardText.text = "+" + rewardAmount;
        rewardText.color = Color.yellow;
        rewardText.enabled = true;
        rewardText.rectTransform.position = screenPos;

        startPosition = screenPos;
        startColor = rewardText.color;
        timer = 0f;
        isActive = true;

        Debug.Log($"[KillRewardManager] Tampilkan +{rewardAmount} di layar");
    }

    private void Update()
    {
        if (!isActive || rewardText == null)
            return;

        timer += Time.deltaTime;

        // Melayang ke atas
        Vector3 newPos = startPosition;
        newPos.y += floatSpeed * timer;
        rewardText.rectTransform.position = newPos;

        // Fade out pada setengah akhir
        if (timer > displayDuration * 0.5f)
        {
            float fadeProgress = (timer - displayDuration * 0.5f) / (displayDuration * 0.5f);
            rewardText.color = Color.Lerp(startColor, Color.clear, fadeProgress);
        }

        // Selesai, sembunyikan kembali
        if (timer >= displayDuration)
        {
            rewardText.text = string.Empty;
            rewardText.enabled = false;
            isActive = false;
        }
    }
}
