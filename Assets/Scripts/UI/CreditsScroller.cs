using TMPro;
using UnityEngine;

/// <summary>
/// Menggulir teks credits secara vertikal dari offset awal ke offset akhir dengan kecepatan tertentu.
/// </summary>
public class CreditsScroller : MonoBehaviour
{
    [Header("Credits")]
    [Tooltip("Komponen teks (TMP) yang akan digulir.")]
    [SerializeField] private TMP_Text text;

    [Tooltip("Kecepatan gulir (unit per detik).")]
    [SerializeField] private float speed = 50f;

    [Tooltip("Offset awal (posisi Y anchored) teks.")]
    [SerializeField] private float startOffset = -300f;

    [Tooltip("Offset akhir (posisi Y anchored) teks.")]
    [SerializeField] private float endOffset = 300f;

    private RectTransform rectTransform;

    private void Awake()
    {
        if (text != null)
            rectTransform = text.rectTransform;
    }

    private void OnEnable()
    {
        ResetToStart();
    }

    private void Update()
    {
        if (rectTransform == null)
            return;

        Vector2 pos = rectTransform.anchoredPosition;
        // Gunakan unscaledDeltaTime agar tetap bergerak meski Time.timeScale = 0 (mis. saat pause/UI).
        pos.y += speed * Time.unscaledDeltaTime;

        // Hentikan di offset akhir (menangani speed positif maupun negatif).
        if (speed >= 0f)
        {
            if (pos.y >= endOffset)
                pos.y = endOffset;
        }
        else
        {
            if (pos.y <= endOffset)
                pos.y = endOffset;
        }

        rectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// Mengembalikan teks ke offset awal.
    /// </summary>
    public void ResetToStart()
    {
        if (rectTransform == null && text != null)
            rectTransform = text.rectTransform;

        if (rectTransform == null)
            return;

        Vector2 pos = rectTransform.anchoredPosition;
        pos.y = startOffset;
        rectTransform.anchoredPosition = pos;
    }
}
