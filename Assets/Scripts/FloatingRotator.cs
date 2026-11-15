using UnityEngine;

/// <summary>
/// Membuat objek berputar di sumbu Y dan terombang-ambing (naik-turun) seperti mengambang.
/// </summary>
public class FloatingRotator : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Kecepatan rotasi di sumbu Y (derajat/detik).")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Float / Bobbing")]
    [Tooltip("Amplitudo naik-turun (meter).")]
    [SerializeField, Min(0f)] private float floatAmplitude = 0.25f;

    [Tooltip("Frekuensi naik-turun (siklus/detik).")]
    [SerializeField, Min(0f)] private float floatFrequency = 1f;

    private Vector3 startLocalPosition;

    private void Start()
    {
        startLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        // Putar di sumbu Y (world space). Gunakan unscaledDeltaTime agar tetap berputar
        // meski Time.timeScale = 0 (misalnya saat transisi ke scene Result).
        transform.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime, Space.World);

        // Terombang-ambing naik-turun. Gunakan unscaledTime agar gerakan tidak tergantung timeScale.
        float offsetY = Mathf.Sin(Time.unscaledTime * floatFrequency * Mathf.PI * 2f) * floatAmplitude;
        Vector3 pos = startLocalPosition;
        pos.y += offsetY;
        transform.localPosition = pos;
    }
}