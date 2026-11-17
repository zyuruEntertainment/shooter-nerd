using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengelola sensitivitas look dan fire secara terpisah.
/// Mendukung slider yang berupa referensi scene maupun prefab: jika referensi
/// adalah prefab asset, slider akan di-instantiate saat runtime.
/// Perubahan pada salah satu slider ikut memperbarui nilai dan menyinkronkan
/// slider lainnya.
/// </summary>
public class SensitivityManager : MonoBehaviour
{
    public static SensitivityManager Instance { get; private set; }

    [Header("Sliders (scene atau prefab)")]
    [Tooltip("Daftar slider/prefab slider untuk sensitivitas look (gerak kamera).")]
    [SerializeField] private List<Slider> lookSensitivitySliders = new List<Slider>();
    [Tooltip("Daftar slider/prefab slider untuk sensitivitas fire (bidikan dari joystick fire).")]
    [SerializeField] private List<Slider> fireSensitivitySliders = new List<Slider>();

    [Header("Parent untuk slider prefab (opsional)")]
    [Tooltip("Transform tempat slider prefab di-instantiate. Jika kosong, memakai transform SensitivityManager.")]
    [SerializeField] private Transform sliderParent;

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

    // Slider yang benar-benar aktif di scene (hasil resolve dari referensi/prefab).
    private readonly List<Slider> activeLookSliders = new List<Slider>();
    private readonly List<Slider> activeFireSliders = new List<Slider>();

    // Instance slider yang dibuat dari prefab (agar bisa dibersihkan saat destroy).
    private readonly List<GameObject> instantiatedSliders = new List<GameObject>();

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

        Transform parent = sliderParent != null ? sliderParent : transform;

        // Look sliders
        if (lookSensitivitySliders != null)
        {
            foreach (Slider sliderRef in lookSensitivitySliders)
            {
                if (sliderRef == null)
                    continue;

                Slider slider = sliderRef;
                if (!slider.gameObject.scene.IsValid())
                {
                    GameObject instance = Instantiate(sliderRef.gameObject, parent);
                    instantiatedSliders.Add(instance);
                    slider = instance.GetComponent<Slider>();
                    if (slider == null)
                        slider = instance.GetComponentInChildren<Slider>();
                }

                if (slider == null)
                    continue;

                slider.minValue = lookMin;
                slider.maxValue = lookMax;
                // Set nilai awal sebelum listener didaftarkan agar handle ikut bergerak.
                slider.value = LookSensitivity;
                slider.onValueChanged.AddListener(OnLookSliderChanged);
                activeLookSliders.Add(slider);
            }
        }

        // Fire sliders
        if (fireSensitivitySliders != null)
        {
            foreach (Slider sliderRef in fireSensitivitySliders)
            {
                if (sliderRef == null)
                    continue;

                Slider slider = sliderRef;
                if (!slider.gameObject.scene.IsValid())
                {
                    GameObject instance = Instantiate(sliderRef.gameObject, parent);
                    instantiatedSliders.Add(instance);
                    slider = instance.GetComponent<Slider>();
                    if (slider == null)
                        slider = instance.GetComponentInChildren<Slider>();
                }

                if (slider == null)
                    continue;

                slider.minValue = fireMin;
                slider.maxValue = fireMax;
                slider.value = FireSensitivity;
                slider.onValueChanged.AddListener(OnFireSliderChanged);
                activeFireSliders.Add(slider);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (Slider slider in activeLookSliders)
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(OnLookSliderChanged);
        }

        foreach (Slider slider in activeFireSliders)
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(OnFireSliderChanged);
        }

        foreach (GameObject go in instantiatedSliders)
        {
            if (go != null)
                Destroy(go);
        }
        instantiatedSliders.Clear();
        activeLookSliders.Clear();
        activeFireSliders.Clear();

        if (Instance == this)
            Instance = null;
    }

    private void OnLookSliderChanged(float value)
    {
        LookSensitivity = Mathf.Clamp(value, lookMin, lookMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);

        foreach (Slider slider in activeLookSliders)
        {
            if (slider != null)
                slider.SetValueWithoutNotify(LookSensitivity);
        }
    }

    private void OnFireSliderChanged(float value)
    {
        FireSensitivity = Mathf.Clamp(value, fireMin, fireMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetFireSensitivity(FireSensitivity);

        foreach (Slider slider in activeFireSliders)
        {
            if (slider != null)
                slider.SetValueWithoutNotify(FireSensitivity);
        }
    }

    public void SetLookSensitivity(float value)
    {
        LookSensitivity = Mathf.Clamp(value, lookMin, lookMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetLookSensitivity(LookSensitivity);

        foreach (Slider slider in activeLookSliders)
        {
            if (slider != null)
                slider.SetValueWithoutNotify(LookSensitivity);
        }
    }

    public void SetFireSensitivity(float value)
    {
        FireSensitivity = Mathf.Clamp(value, fireMin, fireMax);
        if (SensitivitySettings.Instance != null)
            SensitivitySettings.Instance.SetFireSensitivity(FireSensitivity);

        foreach (Slider slider in activeFireSliders)
        {
            if (slider != null)
                slider.SetValueWithoutNotify(FireSensitivity);
        }
    }
}