using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ArcadeWeaponController : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private string weaponName = "AK-47";
    [SerializeField, Min(1)] private int magazineSize = 6;
    [SerializeField, Min(0)] private int startingReserveAmmo = 114;
    [SerializeField, Min(0)] private int startingMagazineAmmo = 6;

    [Header("Game Balance")]
    [SerializeField, Min(0.01f)] private float fireInterval = 0.15f;
    [SerializeField, Min(0.1f)] private float damage = 20f;
    [SerializeField, Min(1f)] private float range = 100f;
    [SerializeField, Min(0.01f)] private float shootingAnimationDuration = 0.2f;

    [Header("References")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Image crosshair;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private PlayerSpriteController spriteController;

    [Header("Target Detection")]
    [SerializeField] private LayerMask hitboxMask = ~0;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;
    private bool mobileFireHeld;
    private Coroutine shootingRoutine;

    private readonly Color normalCrosshairColor = Color.black;
    private readonly Color targetCrosshairColor = Color.red;

    private void Awake()
    {
        currentAmmo = Mathf.Clamp(startingMagazineAmmo, 0, magazineSize);
        reserveAmmo = startingReserveAmmo;

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (spriteController == null)
            spriteController = GetComponent<PlayerSpriteController>();

        KeepCrosshairCentered();

        if (aimCamera == null)
            Debug.LogError("Weapon: Camera belum dipasang.", this);

        UpdateAmmoUI();
    }

    private void Update()
    {
        KeepCrosshairCentered();
        UpdateCrosshair();

        bool mouseHeld =
            Mouse.current != null &&
            Mouse.current.leftButton.isPressed;

        if (mouseHeld || mobileFireHeld)
            Fire();

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            Reload();
        }
    }

    // Fire joystick hanya mengontrol status menembak.
    // Arah bidik selalu mengikuti pusat kamera / crosshair.
    public void SetFireHeld(bool value)
    {
        mobileFireHeld = value;
    }

    private Ray GetCenterAimRay()
    {
        if (aimCamera == null)
            return default;

        return aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    private bool TryGetTarget(out RaycastHit hit)
    {
        hit = default;

        if (aimCamera == null)
            return false;

        return Physics.Raycast(
            GetCenterAimRay(),
            out hit,
            range,
            hitboxMask,
            QueryTriggerInteraction.Collide
        );
    }

    public void Fire()
    {
        if (currentAmmo <= 0 || aimCamera == null)
            return;

        float fireSensitivity = SensitivitySettings.Instance != null
            ? SensitivitySettings.Instance.FireSensitivity
            : 1f;

        float interval = fireInterval / fireSensitivity;

        if (Time.time < nextFireTime)
            return;

        currentAmmo--;
        nextFireTime = Time.time + interval;

        UpdateAmmoUI();
        PlayShootingAnimation();

        // Damage memakai ray yang sama dengan pemeriksaan crosshair.
        if (!TryGetTarget(out RaycastHit hit))
            return;

        HitZone zone = hit.collider.GetComponentInParent<HitZone>();
        if (zone != null)
        {
            zone.ApplyDamage(damage);
            return;
        }

        CombatHealth health = hit.collider.GetComponentInParent<CombatHealth>();
        CombatHealth ownHealth = GetComponent<CombatHealth>();

        if (health != null && health != ownHealth)
            health.TakeDamage(damage);
    }

    private void UpdateCrosshair()
    {
        if (crosshair == null)
            return;

        bool targetingEnemy = false;

        if (TryGetTarget(out RaycastHit hit))
        {
            HitZone zone = hit.collider.GetComponentInParent<HitZone>();
            CombatHealth health = hit.collider.GetComponentInParent<CombatHealth>();

            targetingEnemy =
                zone != null ||
                (health != null && health != GetComponent<CombatHealth>());
        }

        crosshair.color = targetingEnemy
            ? targetCrosshairColor
            : normalCrosshairColor;
    }

    private void KeepCrosshairCentered()
    {
        if (crosshair == null)
            return;

        RectTransform rect = crosshair.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    public void Reload()
    {
        if (currentAmmo >= magazineSize || reserveAmmo <= 0)
            return;

        int needed = magazineSize - currentAmmo;
        int transferred = Mathf.Min(needed, reserveAmmo);

        currentAmmo += transferred;
        reserveAmmo -= transferred;

        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = $"{currentAmmo}/{reserveAmmo}";
    }

    private void PlayShootingAnimation()
    {
        if (spriteController == null)
            return;

        if (shootingRoutine != null)
            StopCoroutine(shootingRoutine);

        shootingRoutine = StartCoroutine(ShootingAnimation());
    }

    private IEnumerator ShootingAnimation()
    {
        spriteController.SetShooting(true);
        yield return new WaitForSeconds(shootingAnimationDuration);
        spriteController.SetShooting(false);
        shootingRoutine = null;
    }

    private void OnDisable()
    {
        mobileFireHeld = false;

        if (shootingRoutine != null)
        {
            StopCoroutine(shootingRoutine);
            shootingRoutine = null;
        }

        if (spriteController != null)
            spriteController.SetShooting(false);
    }
}
