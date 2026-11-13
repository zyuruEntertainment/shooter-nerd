using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ArcadeWeaponController : MonoBehaviour
{
    private string weaponName = "AK-47";
    private int magazineSize = 6;

    private float fireInterval = 0.15f;
    private float damage = 20f;
    private float range = 100f;
    private float shootingAnimationDuration = 0.2f;

    [Header("References")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Image crosshair;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private PlayerSpriteController spriteController;

    [Header("Input")]
    [Tooltip("Jika true, senjata otomatis menembak/reload dari input mouse (untuk player). Bot harus false agar tidak ikut input global.")]
    [SerializeField] private bool autoFireFromInput = true;

    public bool AutoFireFromInput
    {
        get => autoFireFromInput;
        set => autoFireFromInput = value;
    }

    [Header("Target Detection")]
    [SerializeField] private LayerMask hitboxMask = ~0;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;
    private bool mobileFireHeld;
    private bool loggedNoWeapon;
    private Coroutine shootingRoutine;

    private readonly Color normalCrosshairColor = Color.black;
    private readonly Color targetCrosshairColor = Color.red;

    public string WeaponName => weaponName;
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    /// <summary>Apakah senjata sedang aktif (telah diterapkan WeaponData).</summary>
    public bool HasWeapon { get; private set; }

    /// <summary>
    /// Reset the fire cooldown timer. Useful when switching weapons.
    /// </summary>
    public void ResetFireTimer()
    {
        nextFireTime = 0f;
    }

    private void Awake()
    {
        // Mulai tanpa senjata default agar inventory awal benar-benar kosong.
        // Senjata hanya akan aktif setelah WeaponManager memanggil ApplyWeaponData.
        ClearWeapon();

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (spriteController == null)
            spriteController = GetComponent<PlayerSpriteController>();

        KeepCrosshairCentered();

        if (aimCamera == null)
            Debug.LogError("Weapon: Kamera belum dipasang.", this);

        UpdateAmmoUI();

        // Sync ammo di semua ArcadeWeaponController pada GameObject yang sama
        // (karena terkadang ada duplikasi di prefab Player).
        SyncAmmoWithSiblings();
    }

    /// <summary>Kosongkan senjata saat ini (digunakan saat inventory kosong).</summary>
    public void ClearWeapon()
    {
        weaponName = "None";
        currentAmmo = 0;
        reserveAmmo = 0;
        nextFireTime = 0f;
        HasWeapon = false;

        UpdateAmmoUI();
    }

    public void ApplyWeaponData(WeaponData data)
    {
        if (data == null)
        {
            ClearWeapon();
            return;
        }

        weaponName = data.WeaponName;
        magazineSize = data.MagazineSize;
        damage = data.Damage;
        fireInterval = data.FireInterval;
        range = data.Range;
        shootingAnimationDuration = data.ShootingAnimationDuration;

        currentAmmo = Mathf.Clamp(data.StartingMagazineAmmo, 1, magazineSize);

        // Hanya set reserveAmmo jika masih 0 (belum pernah diisi).
        // Ini mencegah ammo yang sudah dibeli hilang saat ganti senjata/equip ulang.
        if (reserveAmmo <= 0)
        {
            reserveAmmo = data.StartingReserveAmmo;
        }

        nextFireTime = 0f;
        HasWeapon = true;
        loggedNoWeapon = false;

        UpdateAmmoUI();

        // Sync ke semua controller siblings.
        SyncAmmoWithSiblings();

        Debug.Log($"[ArcadeWeaponController] {gameObject.name} ApplyWeaponData {weaponName}. magazine={currentAmmo}, reserve={reserveAmmo}");
    }

    /// <summary>Tambah reserve ammo (dibeli di shop). Aplikasikan ke semua controller pada GameObject.</summary>
    public void AddAmmo(int amount)
    {
        if (amount <= 0)
            return;

        reserveAmmo += amount;
        UpdateAmmoUI();

        // Sync ke semua controller siblings agar ammo konsisten.
        SyncAmmoWithSiblings();

        Debug.Log($"[ArcadeWeaponController] {gameObject.name} AddAmmo +{amount}. Reserve={reserveAmmo}");
    }

    /// <summary>Set jumlah ammo (digunakan untuk memastikan bot bisa menembak setelah membeli/ganti senjata).</summary>
    public void SetAmmo(int amount)
    {
        currentAmmo = Mathf.Clamp(amount, 0, magazineSize);
        UpdateAmmoUI();
        SyncAmmoWithSiblings();
    }

    /// <summary>Sinkronkan ammo dengan semua ArcadeWeaponController lain pada GameObject yang sama.</summary>
    private void SyncAmmoWithSiblings()
    {
        var controllers = GetComponents<ArcadeWeaponController>();
        if (controllers.Length <= 1)
            return;

        foreach (var ctrl in controllers)
        {
            if (ctrl != this)
            {
                ctrl.reserveAmmo = this.reserveAmmo;
                ctrl.currentAmmo = this.currentAmmo;
                ctrl.HasWeapon = this.HasWeapon;
                ctrl.weaponName = this.weaponName;
                ctrl.UpdateAmmoUI();
            }
        }
    }

    private void Update()
    {
        // Jangan proses tembak/reload saat game di-pause.
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        KeepCrosshairCentered();
        UpdateCrosshair();

        bool mouseHeld = autoFireFromInput && Mouse.current != null && Mouse.current.leftButton.isPressed;

        if (mouseHeld || mobileFireHeld)
            Fire();

        if (autoFireFromInput && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            Reload();
    }

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
        if (!HasWeapon || currentAmmo <= 0 || aimCamera == null)
        {
            if (!HasWeapon && aimCamera != null && !loggedNoWeapon)
            {
                loggedNoWeapon = true;
                Debug.LogWarning($"[ArcadeWeaponController] {gameObject.name} Fire dibatalkan: HasWeapon=false (belum beli senjata)");
            }
            return;
        }

        float fireSensitivity = SensitivitySettings.Instance != null
            ? SensitivitySettings.Instance.FireSensitivity
            : 1f;

        float interval = fireInterval / Mathf.Max(0.01f, fireSensitivity);

        if (Time.time < nextFireTime)
            return;

        currentAmmo--;
        nextFireTime = Time.time + interval;

        UpdateAmmoUI();
        PlayShootingAnimation();

        if (!TryGetTarget(out RaycastHit hit))
            return;

        CombatHealth ownHealth = GetComponent<CombatHealth>();
        HitZone zone = hit.collider.GetComponentInParent<HitZone>();

        if (zone != null)
        {
            zone.ApplyDamage(damage, ownHealth);
            return;
        }

        CombatHealth health = hit.collider.GetComponentInParent<CombatHealth>();

        if (health != null && health != ownHealth)
            health.TakeDamage(damage, ownHealth);
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

            targetingEnemy = zone != null ||
                (health != null && health != GetComponent<CombatHealth>());
        }

        crosshair.color = targetingEnemy ? targetCrosshairColor : normalCrosshairColor;
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

        int transferred = Mathf.Min(magazineSize - currentAmmo, reserveAmmo);
        currentAmmo += transferred;
        reserveAmmo -= transferred;

        UpdateAmmoUI();
        SyncAmmoWithSiblings();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo}/{reserveAmmo}";
            Debug.Log($"[ArcadeWeaponController] {gameObject.name} UpdateAmmoUI: {currentAmmo}/{reserveAmmo}");
        }
        else
        {
            Debug.LogWarning($"[ArcadeWeaponController] {gameObject.name} UpdateAmmoUI: ammoText is NULL! Assign TMP_Text di Inspector.");
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
