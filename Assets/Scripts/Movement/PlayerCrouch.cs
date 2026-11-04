using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(SphereCollider))]
public class PlayerCrouch : MonoBehaviour
{
    [Header("Crouch Settings")]
    [SerializeField, Min(0f)] private float heightReduction = 1f;
    [SerializeField] private Key crouchKey = Key.LeftCtrl;
    [SerializeField] private bool holdToCrouch = true;

    [Header("References")]
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private SphereCollider sphereCollider;
    [SerializeField] private CharacterController characterController;

    private float originalCapsuleHeight;
    private Vector3 originalCapsuleCenter;
    private Vector3 originalSphereCenter;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;
    private bool initialized;

    public bool IsCrouching { get; private set; }

    private void Awake()
    {
        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();

        if (sphereCollider == null)
            sphereCollider = GetComponent<SphereCollider>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        originalCapsuleHeight = capsuleCollider.height;
        originalCapsuleCenter = capsuleCollider.center;
        originalSphereCenter = sphereCollider.center;

        if (characterController != null)
        {
            originalControllerHeight = characterController.height;
            originalControllerCenter = characterController.center;
        }

        initialized = true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        bool keyHeld = keyboard != null && keyboard[crouchKey].isPressed;
        bool keyPressed = keyboard != null && keyboard[crouchKey].wasPressedThisFrame;

        if (holdToCrouch)
            SetCrouching(keyHeld);
        else if (keyPressed)
            SetCrouching(!IsCrouching);
    }

    // Untuk UI mobile: panggil true saat tombol ditekan, false saat dilepas.
    public void SetCrouching(bool crouching)
    {
        if (!initialized || IsCrouching == crouching)
            return;

        IsCrouching = crouching;

        if (crouching)
        {
            capsuleCollider.height = Mathf.Max(
                originalCapsuleHeight - heightReduction,
                capsuleCollider.radius * 2f
            );

            // Turunkan center setengah pengurangan agar bagian bawah tetap di tempat.
            capsuleCollider.center =
                originalCapsuleCenter + Vector3.down * (heightReduction * 0.5f);

            // Sesuai permintaan: center Z SphereCollider dikurangi 1.
            sphereCollider.center = originalSphereCenter + new Vector3(0f, 0f, -1f);

            // CharacterController juga dipendekkan supaya collider gerak ikut crouch.
            if (characterController != null)
            {
                characterController.height = Mathf.Max(
                    originalControllerHeight - heightReduction,
                    characterController.radius * 2f
                );

                characterController.center =
                    originalControllerCenter + Vector3.down * (heightReduction * 0.5f);
            }
        }
        else
        {
            capsuleCollider.height = originalCapsuleHeight;
            capsuleCollider.center = originalCapsuleCenter;
            sphereCollider.center = originalSphereCenter;

            if (characterController != null)
            {
                characterController.height = originalControllerHeight;
                characterController.center = originalControllerCenter;
            }
        }
    }

    private void OnDisable()
    {
        if (initialized)
            SetCrouching(false);
    }
}
