using UnityEngine;

/// <summary>
/// Animasi scale dari 0 ke ukuran asli dengan kurva memantul (bounce),
/// dimulai otomatis saat objek diaktifkan / scene dimulai.
/// </summary>
public class BounceScaleIn : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Durasi animasi (detik).")]
    [SerializeField, Min(0.01f)] private float duration = 0.5f;

    [Tooltip("Kurva skala (0..1). Default bounce jika kosong.")]
    [SerializeField] private AnimationCurve scaleCurve;

    private Vector3 targetScale;
    private float timer;
    private bool isAnimating;

    private void Awake()
    {
        targetScale = transform.localScale;

        if (scaleCurve == null || scaleCurve.length == 0)
            scaleCurve = CreateDefaultBounceCurve();

        // Mulai dari skala 0.
        transform.localScale = Vector3.zero;
        timer = 0f;
        isAnimating = true;
    }

    private void OnEnable()
    {
        // Ulangi animasi jika objek diaktifkan ulang.
        if (targetScale == Vector3.zero)
            targetScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

        transform.localScale = Vector3.zero;
        timer = 0f;
        isAnimating = true;
    }

    private void Update()
    {
        if (!isAnimating) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float scale = scaleCurve.Evaluate(t);
        transform.localScale = targetScale * scale;

        if (t >= 1f)
            isAnimating = false;
    }

    private static AnimationCurve CreateDefaultBounceCurve()
    {
        // Bounce sederhana: overshoot lalu settle.
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0f, 0f, 0f, 2f));
        curve.AddKey(new Keyframe(0.5f, 1.15f, 0f, 0f));
        curve.AddKey(new Keyframe(0.75f, 0.95f, 0f, 0f));
        curve.AddKey(new Keyframe(1f, 1f, 0f, 0f));
        return curve;
    }
}