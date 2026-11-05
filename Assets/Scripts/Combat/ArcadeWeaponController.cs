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

    [Header("Mobile Fire Joystick Aim")]
    [SerializeField, Range(0.05f, 0.45f)] private float aimViewportRadiusX = 0.28f;
    [SerializeField, Range(0.05f, 0.45f)] private float aimViewportRadiusY = 0.28f;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;
    private bool mobileFireHeld;
    private bool mobileAimActive;
    private Vector2 mobileAimInput;
    private Vector2 defaultCrosshairAnchoredPosition;
    private bool cachedCrosshairPosition;
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

        if (crosshair != null)
        {
            defaultCrosshairAnchoredPosition = crosshair.rectTransform.anchoredPosition;
            cachedCrosshairPosition = true;
        }

        if (aimCamera == null)
            Debug.LogError("Weapon: Camera belum dipasang.", this);

        UpdateAmmoUI();
    }

    private void Update()
    {
        UpdateCrosshair();

        bool mouseHeld =
            Mouse.current != null &&
            Mouse.current.leftButton.isPressed;

        if (mouseHeld || mobileFireHeld)
            Fire();
    }

    public void SetFireHeld(bool value)
    {
        mobileFireHeld = value;
    }

    public void SetMobileAim(Vector2 direction, bool active)
    {
        mobileAimInput = Vector2.ClampMagnitude(direction, 1f);
        mobileAimActive = active;
    }

    private Vector2 GetAimViewportPoint()
    {
        if (!mobileAimActive)
            return new Vector2(0.5f, 0.5f);

        return new Vector2(
            Mathf.Clamp01(0.5f + mobileAimInput.x * aimViewportRadiusX),
            Mathf.Clamp01(0.5f + mobileAimInput.y * aimViewportRadiusY)
        );
    }

    private bool TryGetTarget(out RaycastHit hit)
    {
        hit = default;

        if (aimCamera == null)
            return false;

        Vector2 viewportPoint = GetAimViewportPoint();
        Ray ray = aimCamera.ViewportPointToRay(
            new Vector3(viewportPoint.x, viewportPoint.y, 0f)
        );

        return Physics.Raycast(
            ray,
            out hit,
            range,
            hitboxMask,
            QueryTriggerInteraction.Collide
        );
    }

    public void Fire()
    {
        if (Time.time < nextFireTime || currentAmmo <= 0)
            return;

        currentAmmo--;
        nextFireTime = Time.time + fireInterval;

        UpdateAmmoUI();
        PlayShootingAnimation();

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

        UpdateCrosshairPosition();
    }

    private void UpdateCrosshairPosition()
    {
        if (crosshair == null)
            return;

        RectTransform crosshairRect = crosshair.rectTransform;

        if (!mobileAimActive)
        {
            if (cachedCrosshairPosition)
                crosshairRect.anchoredPosition = defaultCrosshairAnchoredPosition;
            return;
        }

        RectTransform parentRect = crosshairRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Vector2 viewportPoint = GetAimViewportPoint();
        Vector2 screenPoint = new Vector2(
            viewportPoint.x * Screen.width,
            viewportPoint.y * Screen.height
        );

        Canvas canvas = crosshair.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null &&
                          canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint))
        {
            crosshairRect.anchoredPosition = localPoint;
        }
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
        mobileAimActive = false;
        mobileAimInput = Vector2.zero;

        if (shootingRoutine != null)
        {
            StopCoroutine(shootingRoutine);
            shootingRoutine = null;
        }

        if (spriteController != null)
            spriteController.SetShooting(false);
    }
}
