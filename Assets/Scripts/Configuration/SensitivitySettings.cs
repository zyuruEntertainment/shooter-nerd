using UnityEngine;

public class SensitivitySettings : MonoBehaviour
{
    public static SensitivitySettings Instance { get; private set; }

    [Header("Look Sensitivity")]
    [Tooltip("Nilai mentah slider (0.1 - 10). Dipetakan ke pengali internal melalui MapLookSensitivity.")]
    [SerializeField, Min(0.01f)]
    private float lookSensitivity = 3f;

    public float LookSensitivity => lookSensitivity;

    public void SetLookSensitivity(float value)
    {
        lookSensitivity = Mathf.Max(0.01f, value);
    }

    [Header("Fire Sensitivity")]
    [SerializeField, Min(0.01f)]
    private float fireSensitivity = 1f;

    public float FireSensitivity => fireSensitivity;

    public void SetFireSensitivity(float value)
    {
        fireSensitivity = Mathf.Max(0.01f, value);
    }

    /// <summary>
    /// Pastikan ada satu instance SensitivitySettings yang persisten
    /// (tidak ikut hancur bersama Pause UI).
    /// </summary>
    public static SensitivitySettings EnsureInstance()
    {
        if (Instance != null)
            return Instance;

#if UNITY_2023_1_OR_NEWER
        var found = Object.FindAnyObjectByType<SensitivitySettings>();
#else
        var found = Object.FindObjectOfType<SensitivitySettings>();
#endif
        if (found != null)
        {
            Instance = found;
            if (found.transform.parent == null)
                DontDestroyOnLoad(found.gameObject);
            return found;
        }

        var go = new GameObject("SensitivitySettings");
        DontDestroyOnLoad(go);
        return go.AddComponent<SensitivitySettings>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Persist hanya jika GameObject ini adalah root (syarat DontDestroyOnLoad).
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Memetakan nilai mentah slider (0.1 - 10) ke pengali internal.
    /// Kurva kuadratik memberi presisi lebih tinggi pada sensitivitas rendah.
    /// </summary>
    public static float MapLookSensitivity(float raw)
    {
        raw = Mathf.Clamp(raw, 0.1f, 10f);
        float t = (raw - 0.1f) / (10f - 0.1f);
        t = t * t; // ease-in
        return Mathf.Lerp(0.1f, 5f, t);
    }
}
