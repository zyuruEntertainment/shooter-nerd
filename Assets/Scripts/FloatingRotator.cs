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
        // Putar di sumbu Y (world space agar tidak terpengaruh rotasi objek).
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Terombang-ambing naik-turun.
        float offsetY = Mathf.Sin(Time.time * floatFrequency * Mathf.PI * 2f) * floatAmplitude;
        Vector3 pos = startLocalPosition;
        pos.y += offsetY;
        transform.localPosition = pos;
    }
}