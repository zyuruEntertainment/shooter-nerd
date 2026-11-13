using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    private bool crouchButtonBound;

    public bool IsCrouching { get; private set; }

    // Ketika true, Update() tidak akan menimpa status jongkok dari input keyboard,
    // sehingga sistem eksternal (misal bot AI) dapat mengatur jongkok via SetCrouching().
    public bool IgnoreInput { get; set; }

    private void Awake()
    {
        if (initialized)
            return;

        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();

        if (sphereCollider == null)
            sphereCollider = GetComponent<SphereCollider>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (capsuleCollider == null || sphereCollider == null)
        {
            Debug.LogError("PlayerCrouch: CapsuleCollider/SphereCollider tidak ditemukan.", this);
            enabled = false;
            return;
        }

        // Pastikan collider berada pada ukuran penuh sebelum menyimpan nilai original.
        // (Mencegah prefab yang tersimpan dalam keadaan crouch.)
        // Tidak mengubah jika heightReduction <= 0.
        originalCapsuleHeight = capsuleCollider.height;
        originalCapsuleCenter = capsuleCollider.center;
        originalSphereCenter = sphereCollider.center;

        if (characterController != null)
        {
            originalControllerHeight = characterController.height;
            originalControllerCenter = characterController.center;
        }

        initialized = true;

        // Hanya Player (tag Player) yang boleh mengontrol tombol crouch.
        if (gameObject.CompareTag("Player"))
            TryBindCrouchButton();
    }

    private void TryBindCrouchButton()
    {
        if (crouchButtonBound)
            return;

        var buttons = FindObjectsByType<Button>();
        foreach (var btn in buttons)
        {
            if (btn != null && btn.gameObject.name == "Crouch")
            {
                btn.onClick.RemoveListener(OnCrouchClicked);
                btn.onClick.AddListener(OnCrouchClicked);
                crouchButtonBound = true;
            }
        }
    }

    private void Update()
    {
        if (gameObject.CompareTag("Player") && !crouchButtonBound)
            TryBindCrouchButton();

        // Jika diinialisasi oleh sistem eksternal (bot), jangan acak-nginputkan status jongkok.
        if (IgnoreInput)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        bool keyHeld = keyboard[crouchKey].isPressed;
        bool keyPressed = keyboard[crouchKey].wasPressedThisFrame;
        bool keyReleased = keyboard[crouchKey].wasReleasedThisFrame;

        if (holdToCrouch)
        {
            // Gunakan edge input agar status dari tombol UI tidak ditimpa setiap frame.
            if (keyPressed)
                SetCrouching(true);
            else if (keyReleased)
                SetCrouching(false);
        }
        else if (keyPressed)
        {
            SetCrouching(!IsCrouching);
        }
    }

    /// <summary>Toggle status crouch. Dipanggil dari Button OnClick.</summary>
    public void OnCrouchClicked()
    {
        if (!initialized)
            Awake();

        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();
        if (sphereCollider == null)
            sphereCollider = GetComponent<SphereCollider>();
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (capsuleCollider == null || sphereCollider == null)
        {
            Debug.LogError("PlayerCrouch: collider tidak ditemukan.", this);
            return;
        }

        SetCrouching(!IsCrouching);
    }

    public void SetCrouching(bool crouching)
    {
        if (!initialized)
            Awake();

        if (!initialized || IsCrouching == crouching)
            return;

        if (capsuleCollider == null || sphereCollider == null)
            return;

        IsCrouching = crouching;

        if (crouching)
        {
            capsuleCollider.height = Mathf.Max(
                originalCapsuleHeight - heightReduction,
                capsuleCollider.radius * 2f
            );

            capsuleCollider.center =
                originalCapsuleCenter + Vector3.down * (heightReduction * 0.5f);

            sphereCollider.center =
                originalSphereCenter + Vector3.down * (heightReduction * 0.5f);

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
