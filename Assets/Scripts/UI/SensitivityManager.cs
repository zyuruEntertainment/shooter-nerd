using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengelola sensitivitas look, fire control, dan fire rate secara terpisah.
/// </summary>
public class SensitivityManager : MonoBehaviour
{
    public static SensitivityManager Instance { get; private set; }

    [Header("Sliders")]
    [Tooltip("Slider untuk sensitivitas look (gerak kamera).")]
    [SerializeField] private Slider lookSensitivitySlider;
    [Tooltip("Slider untuk sensitivitas kontrol fire (gerakan handle).")]
    [SerializeField] private Slider fireControlSensitivitySlider;
    [Tooltip("Slider untuk fire rate (kecepatan tembak).")]
    [SerializeField] private Slider fireSensitivitySlider;

    [Header("Look Sensitivity Range")]
    [SerializeField, Min(0.01f)] private float lookMin = 0.1f;
    [SerializeField, Min(0.01f)] private float lookMax = 10f;
    [SerializeField, Min(0.01f)] private float defaultLook = 3f;

    [Header("Fire Control Sensitivity Range")]
    [SerializeField, Min(0.01f)] private float fireControlMin = 0.1f;
    [SerializeField, Min(0.01f)] private float fireControlMax = 10f;
    [SerializeField, Min(0.01f)] private float defaultFireControl = 1f;

    [Header("Fire Rate Range")]
    [SerializeField, Min(0.01f)] private float fireRateMin = 0.1f;
    [SerializeField, Min(0.01f)] private float fireRateMax = 5f;
    [SerializeField, Min(0.01f)] private float defaultFireRate = 1f;

    public float LookSensitivity { get; private set; }
    public float FireControlSensitivity { get; private set; }
    public float FireRateMultiplier { get; private set; }

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
            FireControlSensitivity = Mathf.Clamp(settings.FireControlSensitivity, fireControlMin, fireControlMax);
            FireRateMultiplier = Mathf.Clamp(settings.FireRateMultiplier, fireRateMin, fireRateMax);
        }
        else
        {
            LookSensitivity = Mathf.Clamp(defaultLook, lookMin, lookMax);
            FireControlSensitivity = Mathf.Clamp(defaultFireControl, fireControlMin, fireControlMax);
            FireRateMultiplier = Mathf.Clamp(defaultFireRate, fireRateMin, fireRateMax);
        }

        if (SensitivitySettings.Instance == null)
            SensitivitySettings.EnsureInstance();

        if (SensitivitySettings.Instance != null)
        {
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);
            SensitivitySettings.Instance.SetFireControlSensitivity(FireControlSensitivity);
            SensitivitySettings.Instance.SetFireRateMultiplier(FireRateMultiplier);
        }

        if (lookSensitivitySlider != null)
        {
            lookSensitivitySlider.minValue = lookMin;
            lookSensitivitySlider.maxValue = lookMax;
            lookSensitivitySlider.SetValueWithoutNotify(LookSensitivity);
            lookSensitivitySlider.onValueChanged.AddListener(OnLookSliderChanged);
        }

        if (fireControlSensitivitySlider != null)
        {
            fireControlSensitivitySlider.minValue = fireControlMin;
            fireControlSensitivitySlider.maxValue = fireControlMax;
            fireControlSensitivitySlider.SetValueWithoutNotify(FireControlSensitivity);
            fireControlSensitivitySlider.onValueChanged.AddListener(OnFireControlSliderChanged);
        }

        if (fireSensitivitySlider != null)
        {
            fireSensitivitySlider.minValue = fireRateMin;
            fireSensitivitySlider.maxValue = fireRateMax;
            fireSensitivitySlider.SetValueWithoutNotify(FireRateMultiplier);
            fireSensitivitySlider.onValueChanged.AddListener(OnFireRateSliderChanged);
        }
    }

    private void OnDestroy()
    {
        if (lookSensitivitySlider != null)
            lookSensitivitySlider.onValueChanged.RemoveListener(OnLookSliderChanged);
        if (fireControlSensitivitySlider != null)
            fireControlSensitivitySlider.onValueChanged.RemoveListener(OnFireControlSliderChanged);
        if (fireSensitivitySlider != null)
            fireSensitivitySlider.onValueChanged.RemoveListener(OnFireRateSliderChanged);

        if (Instance == this)
            Instance = null;
    }

    private void OnLookSliderChanged(float value)
    {
        LookSensitivity = Mathf.Clamp(value, lookMin, lookMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);
    }

    private void OnFireControlSliderChanged(float value)
    {
        FireControlSensitivity = Mathf.Clamp(value, fireControlMin, fireControlMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetFireControlSensitivity(FireControlSensitivity);
    }

    private void OnFireRateSliderChanged(float value)
    {
        FireRateMultiplier = Mathf.Clamp(value, fireRateMin, fireRateMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetFireRateMultiplier(FireRateMultiplier);
    }

    public void SetLookSensitivity(float value)
    {
        LookSensitivity = Mathf.Clamp(value, lookMin, lookMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);
        if (lookSensitivitySlider != null)
            lookSensitivitySlider.value = LookSensitivity;
    }

    public void SetFireControlSensitivity(float value)
    {
        FireControlSensitivity = Mathf.Clamp(value, fireControlMin, fireControlMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetFireControlSensitivity(FireControlSensitivity);
        if (fireControlSensitivitySlider != null)
            fireControlSensitivitySlider.value = FireControlSensitivity;
    }

    public void SetFireRateMultiplier(float value)
    {
        FireRateMultiplier = Mathf.Clamp(value, fireRateMin, fireRateMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetFireRateMultiplier(FireRateMultiplier);
        if (fireSensitivitySlider != null)
            fireSensitivitySlider.value = FireRateMultiplier;
    }
}
