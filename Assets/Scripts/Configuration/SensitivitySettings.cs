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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
