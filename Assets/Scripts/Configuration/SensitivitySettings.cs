using UnityEngine;

public class SensitivitySettings : MonoBehaviour
{
    public static SensitivitySettings Instance { get; private set; }

    [Header("Look Sensitivity")]
    [SerializeField, Min(0.01f)]
    private float lookSensitivity = 6f;

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
        var found = Object.FindFirstObjectByType<SensitivitySettings>();
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
}
