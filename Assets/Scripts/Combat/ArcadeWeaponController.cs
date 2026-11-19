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
    [Tooltip("Origin tembak untuk bot (muzzle/eye). Jika kosong, gunakan transform + offset.")]
    [SerializeField] private Transform aimOrigin;
    [SerializeField] private Image crosshair;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private PlayerSpriteController spriteController;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float shootVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float reloadVolume = 1f;
    [SerializeField, Min(0.1f)] private float soundMaxDistance = 40f;

    [Header("Model")]
    [SerializeField] private Transform weaponModelParent;
    [SerializeField, Min(0f)] private float reloadModelDrop = 0.15f;
    [SerializeField, Min(0.1f)] private float reloadAnimSpeed = 8f;
    private GameObject currentWeaponModel;
    private Vector3 weaponModelParentBaseLocalPos;
    private bool hasWeaponModelParentBase;
    private float targetReloadOffset;
    private float currentReloadOffset;

    [Header("Input")]
    [Tooltip("Jika true, senjata otomatis menembak/reload dari input mouse (untuk player). Bot harus false agar tidak ikut input global.")]
    [SerializeField] private bool autoFireFromInput = true;

    public bool AutoFireFromInput
    {
        get => autoFireFromInput;
        set => autoFireFromInput = value;
    }

    /// <summary>Target aim untuk bot (musuh yang dilacak). Null untuk player (pakai kamera).</summary>
    public void SetBotAimTarget(Transform target) { botAimTarget = target; }

    /// <summary>Origin aim/muzzle untuk bot.</summary>
    public void SetAimOrigin(Transform origin) { if (origin != null) aimOrigin = origin; }

    /// <summary>Apakah ada sumber aim yang valid (kamera untuk player, target untuk bot).</summary>
    public bool HasAimSource => aimCamera != null || botAimTarget != null;

    /// <summary>Posisi origin tembak.</summary>
    public Vector3 GetAimOriginPosition()
    {
        if (aimOrigin != null) return aimOrigin.position;
        return transform.position + Vector3.up * 1.2f;
    }

    /// <summary>Apakah target dalam jangkauan senjata.</summary>
    public bool IsTargetInRange(Transform target)
    {
        if (target == null) return false;
        float dist = Vector3.Distance(GetAimOriginPosition(), target.position);
        return dist <= range;
    }

    [Header("Target Detection")]
    [SerializeField] private LayerMask hitboxMask = ~0;

    private int currentAmmo;
    private int reserveAmmo;
    private bool infiniteAmmo;
    private AudioClip shootSound;
    private AudioClip reloadSound;

    private float reloadTime = 1f;
    private bool isReloading;
    private float reloadTimer;
    private float nextFireTime;
    private bool mobileFireHeld;
    private bool loggedNoWeapon;
    private Transform botAimTarget;
    private Coroutine shootingRoutine;
    private bool isBot;

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

        isBot = GetComponent<BotController>() != null;
        if (isBot)
        {
            // Bot tidak boleh menggunakan kamera player untuk aiming.
            aimCamera = null;
            autoFireFromInput = false;
        }

        if (aimCamera == null && autoFireFromInput && !isBot)
            aimCamera = Camera.main;

        if (spriteController == null)
            spriteController = GetComponent<PlayerSpriteController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Volume diatur manual berdasarkan jarak ke kamera player.
        if (audioSource != null)
            audioSource.spatialBlend = 0f;

        if (weaponModelParent == null)
        {
            var anchor = GameObject.Find("WeaponModelAnchor");
            if (anchor != null)
                weaponModelParent = anchor.transform;
        }

        if (weaponModelParent != null)
        {
            weaponModelParentBaseLocalPos = weaponModelParent.localPosition;
            hasWeaponModelParentBase = true;
        }

        KeepCrosshairCentered();

        if (aimCamera == null && autoFireFromInput && !isBot)
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
        infiniteAmmo = false;
        nextFireTime = 0f;
        HasWeapon = false;
        shootSound = null;
        reloadSound = null;
        reloadTime = 0f;

        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
            currentWeaponModel = null;
        }
        isReloading = false;
        reloadTimer = 0f;
        targetReloadOffset = 0f;
        currentReloadOffset = 0f;

        if (weaponModelParent != null && hasWeaponModelParentBase)
            weaponModelParent.localPosition = weaponModelParentBaseLocalPos;

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
        shootSound = data.ShootSound;
        reloadSound = data.ReloadSound;
        reloadTime = data.ReloadTime;
        infiniteAmmo = data.InfiniteAmmo;

        // Batalkan reload yang sedang berjalan saat ganti senjata.
        isReloading = false;
        reloadTimer = 0f;
        targetReloadOffset = 0f;
        currentReloadOffset = 0f;

        // Tampilkan model senjata di anchor (jika ada).
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
            currentWeaponModel = null;
        }

        if (weaponModelParent != null && data.WeaponModel != null)
        {
            // Gunakan transform bawaan prefab (posisi/rotasi/skala yang sudah di-set di prefab).
            currentWeaponModel = Instantiate(data.WeaponModel, weaponModelParent);
        }

        currentAmmo = Mathf.Clamp(data.StartingMagazineAmmo, 1, magazineSize);

        // Reset reserveAmmo ke nilai awal senjata ini; WeaponManager akan menimpa
        // dengan nilai persisten (reserveAmmoByWeapon) via SetReserveAmmo setelah ApplyWeaponData.
        // Jangan pertahankan nilai reserve dari senjata sebelumnya (mencegah "rifle jadi sniper ammo").
        if (infiniteAmmo)
            reserveAmmo = int.MaxValue;
        else
            reserveAmmo = data.StartingReserveAmmo;

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

    /// <summary>Set jumlah reserve ammo (digunakan oleh WeaponManager untuk persistensi per senjata).</summary>
    public void SetReserveAmmo(int amount)
    {
        reserveAmmo = Mathf.Max(0, amount);
        UpdateAmmoUI();
        SyncAmmoWithSiblings();
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
                ctrl.infiniteAmmo = this.infiniteAmmo;
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

        // Progres reload (jika sedang reload).
        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
                FinishReload();
        }

        // Animasi smooth anchor turun/naik sesuai target offset.
        if (weaponModelParent != null && hasWeaponModelParentBase)
        {
            currentReloadOffset = Mathf.MoveTowards(
                currentReloadOffset,
                targetReloadOffset,
                reloadAnimSpeed * Time.deltaTime
            );

            Vector3 pos = weaponModelParentBaseLocalPos;
            pos.y += currentReloadOffset;
            weaponModelParent.localPosition = pos;
        }

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
        if (aimCamera != null)
            return aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Bot: arahkan dari origin ke target.
        Vector3 origin = GetAimOriginPosition();
        Vector3 dir = transform.forward;
        if (botAimTarget != null)
        {
            Vector3 to = botAimTarget.position - origin;
            if (to.sqrMagnitude > 0.0001f) dir = to.normalized;
        }
        return new Ray(origin, dir);
    }

    private bool TryGetTarget(out RaycastHit hit)
    {
        hit = default;

        if (aimCamera != null)
        {
            return Physics.Raycast(
                GetCenterAimRay(),
                out hit,
                range,
                hitboxMask,
                QueryTriggerInteraction.Collide
            );
        }

        // Bot: dedicated hit detection. RaycastAll, skip self colliders, first hit wins.
        if (botAimTarget == null)
            return false;

        Vector3 origin = GetAimOriginPosition();
        Vector3 toTarget = botAimTarget.position - origin;
        float targetDist = toTarget.magnitude;
        if (targetDist <= 0.001f) return false;

        Vector3 dir = toTarget / targetDist;
        float rayDist = Mathf.Min(range, targetDist + 0.5f);

        // Sertakan layer default (0) agar player (layer 0) dan obstacle dapat terdeteksi.
        int mask = hitboxMask.value | 1;
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, rayDist, mask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            Transform ht = h.transform;

            // Abaikan semua collider milik bot penembak.
            if (ht == transform || ht.IsChildOf(transform) || transform.IsChildOf(ht))
                continue;

            hit = h;
            return true;
        }

        return false;
    }

    public void Fire()
    {
        if (!MatchManager.ShootingEnabled || !HasWeapon || currentAmmo <= 0 || isReloading || !HasAimSource)
        {
            if (!HasWeapon && !loggedNoWeapon)
            {
                loggedNoWeapon = true;
                Debug.LogWarning($"[ArcadeWeaponController] {gameObject.name} Fire dibatalkan: HasWeapon=false (belum beli senjata)");
            }
            return;
        }

        float interval = fireInterval;

        if (Time.time < nextFireTime)
            return;

        if (!infiniteAmmo)
            currentAmmo--;
        nextFireTime = Time.time + interval;

        UpdateAmmoUI();
        PlayShootingAnimation();

        PlayWeaponSound(shootSound, shootVolume);

        if (!TryGetTarget(out RaycastHit hit))
            return;

        // Diagnostik terkontrol: target raycast aktual saat bot menembak.
        if (botAimTarget != null && aimCamera == null)
        {
            CombatHealth resolvedHealth = hit.collider != null ? hit.collider.GetComponentInParent<CombatHealth>() : null;
            HitZone resolvedZone = hit.collider != null ? hit.collider.GetComponentInParent<HitZone>() : null;
            CombatHealth owner = resolvedZone != null ? resolvedZone.Owner : resolvedHealth;
            Debug.Log($"[BotWeapon] {gameObject.name}: intended={botAimTarget.name}, hit={(hit.collider != null ? hit.collider.name : "none")}, owner={(owner != null ? owner.gameObject.name : "none")} (team {(owner != null ? owner.Team.ToString() : "None")}).", this);
        }

        CombatHealth ownHealth = GetComponentInParent<CombatHealth>();
        HitZone zone = hit.collider.GetComponentInParent<HitZone>();

        if (zone != null)
        {
            // Hindari self-damage.
            if (zone.Owner != null && zone.Owner == ownHealth)
                return;
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

            CombatHealth ownHealth = GetComponentInParent<CombatHealth>();
            bool isEnemyTeam = health != null && health != ownHealth &&
                ownHealth != null && ownHealth.Team != Team.None &&
                health.Team != Team.None && ownHealth.Team != health.Team;

            // HitZone hanya dianggap musuh jika owner-nya musuh (team validation sama).
            bool zoneIsEnemy = false;
            if (zone != null && zone.Owner != null && ownHealth != null)
            {
                zoneIsEnemy = zone.Owner != ownHealth &&
                    ownHealth.Team != Team.None && zone.Owner.Team != Team.None &&
                    ownHealth.Team != zone.Owner.Team;
            }

            targetingEnemy = zoneIsEnemy || isEnemyTeam;
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
        if (!HasWeapon || isReloading || currentAmmo >= magazineSize || reserveAmmo <= 0)
            return;

        // Jika waktu reload 0, langsung selesai.
        if (reloadTime <= 0f)
        {
            PlayWeaponSound(reloadSound, reloadVolume);
            FinishReload();
            return;
        }

        isReloading = true;
        reloadTimer = reloadTime;
        targetReloadOffset = -reloadModelDrop;

        PlayWeaponSound(reloadSound, reloadVolume);
    }

    /// <summary>Putar sound dengan volume berdasarkan jarak ke kamera player.</summary>
    private void PlayWeaponSound(AudioClip clip, float baseVolume)
    {
        if (audioSource == null || clip == null)
            return;

        float volume = Mathf.Clamp01(baseVolume);

        Camera cam = Camera.main;
        if (cam != null && soundMaxDistance > 0f)
        {
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            float factor = Mathf.Clamp01(1f - (dist / soundMaxDistance));
            volume *= factor;
        }

        audioSource.PlayOneShot(clip, volume);
    }

    /// <summary>Selesaikan reload: pindahkan ammo dari reserve ke magazine.</summary>
    public void FinishReload()
    {
        if (!HasWeapon || currentAmmo >= magazineSize || reserveAmmo <= 0)
        {
            isReloading = false;
            reloadTimer = 0f;
            return;
        }

        int transferred = Mathf.Min(magazineSize - currentAmmo, reserveAmmo);
        currentAmmo += transferred;
        reserveAmmo -= transferred;

        isReloading = false;
        reloadTimer = 0f;
        targetReloadOffset = 0f;

        UpdateAmmoUI();
        SyncAmmoWithSiblings();

        // Sinkronkan reserve ke WeaponManager jika controller ini milik player.
        if (WeaponManager.Instance != null && WeaponManager.Instance.ActiveWeaponController == this)
        {
            WeaponManager.Instance.SetReserveAmmo(WeaponManager.Instance.EquippedWeapon, reserveAmmo);
        }
    }

    /// <summary>Apakah sedang dalam proses reload.</summary>
    public bool IsReloading => isReloading;

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
