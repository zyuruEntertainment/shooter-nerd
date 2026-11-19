using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fade in/out untuk sebuah Canvas dengan memanipulasi alpha pada komponen CanvasGroup.
/// Untuk intro: canvas muncul (fade in), tahan, lalu hilang (fade out), kemudian
/// dilempar ke scene tujuan (default: "Menu").
/// </summary>
public class CanvasGroupFade : MonoBehaviour
{
    [Header("Canvas Group")]
    [Tooltip("CanvasGroup yang akan di-fade. Jika kosong, dicari otomatis pada GameObject ini / children.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Durasi (detik)")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Alur Intro")]
    [Tooltip("Jalankan alur intro (fade in -> tahan -> fade out -> pindah scene) saat Start.")]
    [SerializeField] private bool playIntroOnStart = true;
    [Tooltip("Nama scene tujuan setelah fade out selesai.")]
    [SerializeField] private string targetSceneName = "Menu";
    [Tooltip("Gunakan scene 'Loading' terlebih dahulu sebelum scene tujuan.")]
    [SerializeField] private bool useLoadingScreen = false;
    [Tooltip("Nonaktifkan GameObject ini setelah fade out selesai (sebelum pindah scene).")]
    [SerializeField] private bool disableAfterFadeOut = true;

    [Header("Interaksi")]
    [Tooltip("BlocksRaycasts hanya saat canvas terlihat (alpha > 0).")]
    [SerializeField] private bool manageRaycasts = true;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>(true);
    }

    private void Start()
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("[CanvasGroupFade] CanvasGroup tidak ditemukan. Komponen dinonaktifkan.");
            enabled = false;
            return;
        }

        // Mulai dari transparan.
        canvasGroup.alpha = 0f;
        SetInteractable(false);

        if (playIntroOnStart)
            PlayIntro();
    }

    /// <summary>Menjalankan alur intro lengkap: fade in -> tahan -> fade out -> pindah scene.</summary>
    public void PlayIntro()
    {
        StopCurrent();
        currentRoutine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        // Fade in: muncul.
        yield return Fade(0f, 1f, fadeInDuration);
        SetInteractable(true);

        // Tahan sejenak.
        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        SetInteractable(false);

        // Fade out: hilang.
        yield return Fade(1f, 0f, fadeOutDuration);

        if (disableAfterFadeOut)
            gameObject.SetActive(false);

        // Lempar ke scene tujuan.
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            if (useLoadingScreen)
                LoadingScreen.Load(targetSceneName);
            else
                SceneManager.LoadScene(targetSceneName);
        }
    }

    /// <summary>Fade in (alpha 0 -> 1).</summary>
    public void FadeIn()
    {
        StopCurrent();
        currentRoutine = StartCoroutine(FadeRoutine(0f, 1f, fadeInDuration, true));
    }

    /// <summary>Fade out (alpha 1 -> 0).</summary>
    public void FadeOut()
    {
        StopCurrent();
        currentRoutine = StartCoroutine(FadeRoutine(1f, 0f, fadeOutDuration, false));
    }

    /// <summary>Fade ke nilai alpha tertentu.</summary>
    public void FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null) return;
        StopCurrent();
        currentRoutine = StartCoroutine(Fade(canvasGroup.alpha, Mathf.Clamp01(targetAlpha), duration));
        if (targetAlpha > 0f)
            SetInteractable(true);
        else
            SetInteractable(false);
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, bool makeInteractableAfter)
    {
        yield return Fade(from, to, duration);
        if (makeInteractableAfter && to > 0f)
            SetInteractable(true);
        else if (!makeInteractableAfter && to <= 0f)
            SetInteractable(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void SetInteractable(bool value)
    {
        if (canvasGroup == null || !manageRaycasts) return;
        canvasGroup.interactable = value;
        canvasGroup.blocksRaycasts = value;
    }

    private void StopCurrent()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
    }
}
