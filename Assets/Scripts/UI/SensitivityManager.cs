using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengelola sensitivitas look dan fire secara terpisah.
/// </summary>
public class SensitivityManager : MonoBehaviour
{
    public static SensitivityManager Instance { get; private set; }

    [Header("Sliders")]
    [Tooltip("Slider untuk sensitivitas look (gerak kamera).")]
    [SerializeField] private Slider lookSensitivitySlider;
    [Tooltip("Slider untuk sensitivitas fire (bidikan dari joystick fire).")]
    [SerializeField] private Slider fireSensitivitySlider;

    [Header("Look Sensitivity Range")]
    [SerializeField, Min(0.01f)] private float lookMin = 0.1f;
    [SerializeField, Min(0.01f)] private float lookMax = 10f;
    [SerializeField, Min(0.01f)] private float defaultLook = 3f;

    [Header("Fire Sensitivity Range")]
    [SerializeField, Min(0.01f)] private float fireMin = 0.1f;
    [SerializeField, Min(0.01f)] private float fireMax = 10f;
    [SerializeField, Min(0.01f)] private float defaultFire = 1f;

    public float LookSensitivity { get; private set; }
    public float FireSensitivity { get; private set; }

    private bool initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SensitivitySettings.EnsureInstance();
    }

    private void Start()
    {
        if (initialized)
            return;
        initialized = true;

        var settings = SensitivitySettings.Instance;
        if (settings != null)
        {
            LookSensitivity = Mathf.Clamp(settings.LookSensitivity, lookMin, lookMax);
            FireSensitivity = Mathf.Clamp(settings.FireSensitivity, fireMin, fireMax);
        }
        else
        {
            LookSensitivity = Mathf.Clamp(defaultLook, lookMin, lookMax);
            FireSensitivity = Mathf.Clamp(defaultFire, fireMin, fireMax);
        }

        if (SensitivitySettings.Instance == null)
            SensitivitySettings.EnsureInstance();

        if (SensitivitySettings.Instance != null)
        {
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);
            SensitivitySettings.Instance.SetFireSensitivity(FireSensitivity);
        }

        if (lookSensitivitySlider != null)
        {
            lookSensitivitySlider.minValue = lookMin;
            lookSensitivitySlider.maxValue = lookMax;
            lookSensitivitySlider.SetValueWithoutNotify(LookSensitivity);
            lookSensitivitySlider.onValueChanged.AddListener(OnLookSliderChanged);
        }

        if (fireSensitivitySlider != null)
        {
            fireSensitivitySlider.minValue = fireMin;
            fireSensitivitySlider.maxValue = fireMax;
            fireSensitivitySlider.SetValueWithoutNotify(FireSensitivity);
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

    public void SetLookSensitivity(float value)
    {
        LookSensitivity = Mathf.Clamp(value, lookMin, lookMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);
        if (lookSensitivitySlider != null)
            lookSensitivitySlider.value = LookSensitivity;
    }

    public void SetFireSensitivity(float value)
    {
        FireSensitivity = Mathf.Clamp(value, fireMin, fireMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetFireSensitivity(FireSensitivity);
        if (fireSensitivitySlider != null)
            fireSensitivitySlider.value = FireSensitivity;
    }
}
