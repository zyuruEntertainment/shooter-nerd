using UnityEngine;

/// <summary>
/// Membuat GameObject (biasanya sprite/billboard) selalu menghadap kamera.
/// Pasang pada objek yang ingin tetap menghadap kamera setiap frame.
/// </summary>
public class FaceCamera : MonoBehaviour
{
    public enum Mode
    {
        /// <summary>Rotasi objek = rotasi kamera (sprite tetap tegak lurus terhadap kamera).</summary>
        MatchCameraRotation,
        /// <summary>Objek memutar untuk menghadap langsung ke kamera (LookAt).</summary>
        LookAtCamera
    }

    [Header("Kamera")]
    [Tooltip("Kamera target. Jika kosong, memakai Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Pengaturan")]
    [SerializeField] private Mode mode = Mode.MatchCameraRotation;
    [Tooltip("Jika true, sumbu Y (atas) tetap tegak (tidak ikut miring kamera).")]
    [SerializeField] private bool lockY = true;
    [Tooltip("Jika true, sprite dibalik 180 derajat pada sumbu Y (berguna untuk SpriteRenderer).")]
    [SerializeField] private bool flip180 = false;

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        switch (mode)
        {
            case Mode.MatchCameraRotation:
                Quaternion rot = targetCamera.transform.rotation;
                if (lockY)
                {
                    Vector3 euler = rot.eulerAngles;
                    euler.x = 0f;
                    euler.z = 0f;
                    rot = Quaternion.Euler(euler);
                }
                if (flip180)
                    rot *= Quaternion.Euler(0f, 180f, 0f);
                transform.rotation = rot;
                break;

            case Mode.LookAtCamera:
                Vector3 direction = targetCamera.transform.position - transform.position;
                if (lockY)
                    direction.y = 0f;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction);
                    if (flip180)
                        lookRot *= Quaternion.Euler(0f, 180f, 0f);
                    transform.rotation = lookRot;
                }
                break;
        }
    }
}