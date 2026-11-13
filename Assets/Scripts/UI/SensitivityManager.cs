using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengelola sensitivitas look dan fire secara terpisah, serta menghubungkan
/// kedua nilai tersebut ke slider UI (look slider dan fire slider).
/// </summary>
public class SensitivityManager : MonoBehaviour
{
    public static SensitivityManager Instance { get; private set; }

    [Header("Sliders")]
    [Tooltip("Slider untuk sensitivitas look (gerak kamera).")]
    [SerializeField] private Slider lookSensitivitySlider;
    [Tooltip("Slider untuk sensitivitas fire (kecepatan tembak).")]
    [SerializeField] private Slider fireSensitivitySlider;

    [Header("Look Sensitivity Range")]
    [SerializeField, Min(0.01f)] private float lookMin = 0.1f;
    [SerializeField, Min(0.01f)] private float lookMax = 10f;
    [SerializeField, Min(0.01f)] private float defaultLook = 6f;

    [Header("Fire Sensitivity Range")]
    [SerializeField, Min(0.01f)] private float fireMin = 0.1f;
    [SerializeField, Min(0.01f)] private float fireMax = 5f;
    [SerializeField, Min(0.01f)] private float defaultFire = 1f;

    public float LookSensitivity { get; private set; }
    public float FireSensitivity { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Pastikan ada SensitivitySettings agar PlayerMovement/ArcadeWeaponController
        // tetap bisa membaca nilai sensitivitas.
        if (SensitivitySettings.Instance == null)
            gameObject.AddComponent<SensitivitySettings>();
    }

    private void Start()
    {
        // Inisialisasi nilai awal.
        LookSensitivity = Mathf.Clamp(defaultLook, lookMin, lookMax);
        FireSensitivity = Mathf.Clamp(defaultFire, fireMin, fireMax);

        // Sinkronkan ke SensitivitySettings.
        if (SensitivitySettings.Instance != null)
        {
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);
            SensitivitySettings.Instance.SetFireSensitivity(FireSensitivity);
        }

        // Siapkan slider look.
        if (lookSensitivitySlider != null)
        {
            lookSensitivitySlider.minValue = lookMin;
            lookSensitivitySlider.maxValue = lookMax;
            lookSensitivitySlider.value = LookSensitivity;
            lookSensitivitySlider.onValueChanged.AddListener(OnLookSliderChanged);
        }

        // Siapkan slider fire.
        if (fireSensitivitySlider != null)
        {
            fireSensitivitySlider.minValue = fireMin;
            fireSensitivitySlider.maxValue = fireMax;
            fireSensitivitySlider.value = FireSensitivity;
            fireSensitivitySlider.onValueChanged.AddListener(OnFireSliderChanged);
        }
    }

    private void OnDestroy()
    {
        if (lookSensitivitySlider != null)
            lookSensitivitySlider.onValueChanged.RemoveListener(OnLookSliderChanged);

        if (fireSensitivitySlider != null)
            fireSensitivitySlider.onValueChanged.RemoveListener(OnFireSliderChanged);

        if (Instance == this)
            Instance = null;
    }

    private void OnLookSliderChanged(float value)
    {
        LookSensitivity = Mathf.Clamp(value, lookMin, lookMax);

        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);
    }

    private void OnFireSliderChanged(float value)
    {
        FireSensitivity = Mathf.Clamp(value, fireMin, fireMax);

        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetFireSensitivity(FireSensitivity);
    }

    /// <summary>Set sensitivitas look secara programatik (juga memperbarui slider).</summary>
    public void SetLookSensitivity(float value)
    {
        LookSensitivity = Mathf.Clamp(value, lookMin, lookMax);

        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);

        if (lookSensitivitySlider != null)
            lookSensitivitySlider.value = LookSensitivity;
    }

    /// <summary>Set sensitivitas fire secara programatik (juga memperbarui slider).</summary>
    public void SetFireSensitivity(float value)
    {
        FireSensitivity = Mathf.Clamp(value, fireMin, fireMax);

        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetFireSensitivity(FireSensitivity);

        if (fireSensitivitySlider != null)
            fireSensitivitySlider.value = FireSensitivity;
    }
}
