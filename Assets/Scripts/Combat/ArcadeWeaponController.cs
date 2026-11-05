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

    private Color normalCrosshairColor = Color.black;
    private Color targetCrosshairColor = Color.red;

    private void Awake()
    {
        currentAmmo = Mathf.Clamp(
            startingMagazineAmmo,
            0,
            magazineSize
        );

        reserveAmmo = startingReserveAmmo;

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (spriteController == null)
            spriteController = GetComponent<PlayerSpriteController>();

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

    public void Fire()
    {
        if (Time.time < nextFireTime)
            return;

        if (currentAmmo <= 0)
            return;

        currentAmmo--;
        nextFireTime = Time.time + fireInterval;

        UpdateAmmoUI();
        PlayShootingAnimation();

        if (TryGetTarget(out RaycastHit hit))
        {
            HitZone zone = hit.collider.GetComponent<HitZone>();

            if (zone != null)
                zone.ApplyDamage(damage);
        }
    }

    private bool TryGetTarget(out RaycastHit hit)
    {
        hit = default;

        if (aimCamera == null)
            return false;

        Ray ray = aimCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        return Physics.Raycast(
            ray,
            out hit,
            range,
            hitboxMask,
            QueryTriggerInteraction.Collide
        );
    }

    private void UpdateCrosshair()
    {
        if (crosshair == null)
            return;

        bool targetingEnemy = false;

        if (TryGetTarget(out RaycastHit hit))
        {
            HitZone zone = hit.collider.GetComponent<HitZone>();
            targetingEnemy = zone != null;
        }

        crosshair.color = targetingEnemy
            ? targetCrosshairColor
            : normalCrosshairColor;
    }

    public void Reload()
    {
        if (currentAmmo >= magazineSize)
            return;

        if (reserveAmmo <= 0)
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
        {
            ammoText.text = $"{currentAmmo}/{reserveAmmo}";
        }
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

        yield return new WaitForSeconds(
            shootingAnimationDuration
        );

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