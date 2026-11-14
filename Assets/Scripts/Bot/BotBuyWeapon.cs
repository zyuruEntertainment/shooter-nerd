using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Satu script untuk bot membeli senjata dari shop menggunakan ArcadeWeaponController
/// (sama sistem combat seperti player). Bot mulai dengan inventory kosong dan 2500 credits.
/// Bot akan mencoba membeli senjata yang berbeda jika sebelumnya sudah dibeli,
/// dan secara berkala mengganti senjata yang dimiliki.
/// </summary>
public class BotBuyWeapon : MonoBehaviour
{
    [Header("Economy")]
    [SerializeField, Min(0)] private int startingCredits = 2500;

    public int Credits { get; private set; }

    [Header("Catalog (senjata yang bisa dibeli bot dari shop)")]
    [SerializeField] private WeaponData[] availableWeapons = new WeaponData[0];

    [Header("Catalog (ammo yang bisa dibeli bot)")]
    [SerializeField] private AmmoData[] availableAmmos = new AmmoData[0];

    [Header("Referensi")]
    [SerializeField] private ArcadeWeaponController weaponController;

    [Header("Pengaturan Beli")]
    [SerializeField] private float buyCheckInterval = 3f;
    [SerializeField] private bool canBuyWeapons = true;
    [SerializeField] private bool buyImmediatelyOnStart = true;

    [Header("Pengaturan Beli Ammo")]
    [SerializeField] private float ammoBuyCheckInterval = 5f;
    [SerializeField] private bool canBuyAmmo = true;

    [Header("Fallback Ammo (jika katalog kosong)")]
    [SerializeField, Min(0)] private int fallbackAmmoPrice = 100;
    [SerializeField, Min(1)] private int fallbackAmmoAmount = 30;

    [Header("Pengaturan Ganti Senjata")]
    [SerializeField] private float switchWeaponInterval = 10f;
    [SerializeField] private bool canSwitchWeapons = true;

    private readonly List<WeaponData> ownedWeapons = new List<WeaponData>();
    // Reserve ammo per senjata agar tidak hilang saat ganti senjata / beli ammo.
    private readonly Dictionary<WeaponData, int> reserveAmmoByWeapon = new Dictionary<WeaponData, int>();
    // Current (magazine) ammo per senjata agar tidak reset saat ganti senjata.
    private readonly Dictionary<WeaponData, int> currentAmmoByWeapon = new Dictionary<WeaponData, int>();
    private float buyTimer;
    private float ammoBuyTimer;
    private float switchTimer;
    private int currentWeaponIndex = -1;
    private bool hasAttemptedInitialBuy;
    private float _lastSwitchDebugTime = -999f;

    public IReadOnlyList<WeaponData> OwnedWeapons => ownedWeapons;
    public WeaponData CurrentWeapon => currentWeaponIndex >= 0 && currentWeaponIndex < ownedWeapons.Count ? ownedWeapons[currentWeaponIndex] : null;

    private void Awake()
    {
        if (weaponController == null)
            weaponController = GetComponent<ArcadeWeaponController>();

        Debug.Log($"[BotBuyWeapon] {gameObject.name} Awake. weaponController={weaponController != null}, availableWeapons={availableWeapons?.Length ?? 0}, availableAmmos={availableAmmos?.Length ?? 0}");

        // Pastikan pengaturan ammo memiliki nilai yang valid meskipun field tidak terserialisasi di scene.
        if (ammoBuyCheckInterval <= 0f)
            ammoBuyCheckInterval = 5f;
        // Aktifkan pembelian ammo (katalog atau fallback) meskipun field tidak terserialisasi.
        canBuyAmmo = true;

        // Mulai dengan inventory kosong dan kredit 2500 setiap kali Play.
        ownedWeapons.Clear();
        reserveAmmoByWeapon.Clear();
        currentAmmoByWeapon.Clear();
        Credits = startingCredits;
        currentWeaponIndex = -1;

        // Pastikan weapon controller mulai kosong (tanpa senjata default).
        if (weaponController != null)
            weaponController.ClearWeapon();

        Debug.Log($"[BotBuyWeapon] {gameObject.name} Awake selesai. Credits={Credits}, HasWeapon={weaponController?.HasWeapon}");
    }

    private void Start()
    {
        Debug.Log($"[BotBuyWeapon] {gameObject.name} Start. hasAttemptedInitialBuy={hasAttemptedInitialBuy}");

        if (buyImmediatelyOnStart && !hasAttemptedInitialBuy)
        {
            hasAttemptedInitialBuy = true;
            TryBuyWeapon();
        }
    }

    private void Update()
    {
        // --- Logika beli senjata ---
        if (canBuyWeapons && weaponController != null)
        {
            buyTimer -= Time.deltaTime;
            if (buyTimer <= 0f)
            {
                buyTimer = buyCheckInterval;
                TryBuyWeapon();
            }
        }

        // --- Logika beli ammo ---
        if (canBuyAmmo && weaponController != null)
        {
            // Jika senjata aktif kehabisan ammo, percepat pengecekan beli ammo.
            if (weaponController.HasWeapon && weaponController.CurrentAmmo <= 0 && weaponController.ReserveAmmo <= 0)
            {
                ammoBuyTimer = 0f;
            }

            ammoBuyTimer -= Time.deltaTime;
            if (ammoBuyTimer <= 0f)
            {
                ammoBuyTimer = ammoBuyCheckInterval;
                TryBuyAmmo();
            }
        }

        // --- Logika ganti senjata ---
        if (canSwitchWeapons && weaponController != null && ownedWeapons.Count > 1)
        {
            // AUTO-SWITCH: jika senjata aktif ammo 0 DAN reserve 0 (0/0), otomatis ganti senjata lain.
            if (weaponController.HasWeapon && weaponController.CurrentAmmo <= 0 && weaponController.ReserveAmmo <= 0)
            {
                if (Time.time - _lastSwitchDebugTime > 1f)
                {
                    _lastSwitchDebugTime = Time.time;
                    Debug.Log($"[BotBuyWeapon] {gameObject.name} Senjata aktif ammo 0/0, otomatis ganti ke senjata lain.");
                }
                SwitchToNextWeapon();
            }
            else
            {
                switchTimer -= Time.deltaTime;
                if (switchTimer <= 0f)
                {
                    switchTimer = switchWeaponInterval;
                    SwitchToNextWeapon();
                }
            }
        }
    }

    private void TryBuyWeapon()
    {
        var available = availableWeapons;
        if (available == null || available.Length == 0)
        {
            Debug.LogWarning($"[BotBuyWeapon] {gameObject.name} AvailableWeapons KOSONG! Isi WeaponData di Inspector BotBuyWeapon.");
            return;
        }

        Debug.Log($"[BotBuyWeapon] {gameObject.name} Mengecek beli senjata. Credits={Credits}, owned={ownedWeapons.Count}, available={available.Length}");

        // Cari senjata yang BELUM dimiliki dan termurah yang bisa dibeli.
        var affordable = available
            .Where(w => w != null && !ownedWeapons.Contains(w) && Credits >= w.Price)
            .OrderBy(w => w.Price)
            .ToList();

        if (affordable.Count > 0)
        {
            var weapon = affordable[0]; // beli yang termurah yang belum dimiliki

            // Simpan ammo senjata sebelumnya agar tidak hilang.
        if (CurrentWeapon != null && weaponController != null && weaponController.HasWeapon)
        {
            reserveAmmoByWeapon[CurrentWeapon] = weaponController.ReserveAmmo;
            currentAmmoByWeapon[CurrentWeapon] = weaponController.CurrentAmmo;
        }

        // Kurangi kredit.
            Credits -= weapon.Price;

            // Tambah ke inventory.
            ownedWeapons.Add(weapon);
        reserveAmmoByWeapon[weapon] = weapon.StartingReserveAmmo;
        currentAmmoByWeapon[weapon] = weapon.StartingMagazineAmmo;

            // Terapkan ke ArcadeWeaponController (sama seperti player equip).
            weaponController.ApplyWeaponData(weapon);
        weaponController.SetReserveAmmo(reserveAmmoByWeapon[weapon]);
            currentWeaponIndex = ownedWeapons.IndexOf(weapon);

            // Pastikan ammo tidak nol agar bot bisa menembak segera.
            if (weaponController != null && weaponController.CurrentAmmo <= 0)
            {
                weaponController.SetAmmo(Mathf.Max(1, weapon.StartingMagazineAmmo));
            }

            Debug.Log($"[BotBuyWeapon] {gameObject.name} >>> BERHASIL BELI: {weapon.WeaponName} (harga {weapon.Price}, damage {weapon.Damage}, range {weapon.Range}). Sisa kredit {Credits}. HasWeapon={weaponController.HasWeapon}, Ammo={weaponController.CurrentAmmo}");
        }
        else
        {
            // Cek apakah semua senjata sudah dimiliki atau tidak cukup kredit.
            var allOwned = available.All(w => w == null || ownedWeapons.Contains(w));
            var cheapestAvailable = available.Where(w => w != null).OrderBy(w => w.Price).FirstOrDefault();

            if (allOwned)
            {
                Debug.Log($"[BotBuyWeapon] {gameObject.name} Semua senjata sudah dimiliki.");
            }
            else if (cheapestAvailable != null)
            {
                Debug.Log($"[BotBuyWeapon] {gameObject.name} Tidak cukup kredit untuk {cheapestAvailable.WeaponName} (harga {cheapestAvailable.Price}). Credits={Credits}");
            }
            else
            {
                Debug.Log($"[BotBuyWeapon] {gameObject.name} Tidak ada senjata terjangkau. Credits={Credits}");
            }
        }
    }

    /// <summary>Beli ammo untuk senjata yang ammunya habis.</summary>
    private void TryBuyAmmo()
    {
        if (weaponController == null)
            return;

        // Fallback: jika katalog ammo tidak diisi di scene, tetap bisa beli ammo
        // dengan harga/jumlah default agar bot tidak kehabisan ammo.
        if (availableAmmos == null || availableAmmos.Length == 0)
        {
            TryBuyAmmoFallback();
            return;
        }

        Debug.Log($"[BotBuyWeapon] {gameObject.name} TryBuyAmmo: owned={ownedWeapons.Count}, activeReserve={weaponController.ReserveAmmo}, credits={Credits}");

        // Sync reserve senjata aktif agar data tidak stale.
        if (CurrentWeapon != null && weaponController.HasWeapon)
            reserveAmmoByWeapon[CurrentWeapon] = weaponController.ReserveAmmo;

        // Cari senjata (owned) yang reserve ammunya habis.
        WeaponData targetWeapon = null;
        foreach (var w in ownedWeapons)
        {
            int reserve = w.StartingReserveAmmo;
            if (reserveAmmoByWeapon.TryGetValue(w, out var r))
                reserve = r;
            if (w == CurrentWeapon && weaponController.HasWeapon)
                reserve = weaponController.ReserveAmmo;

            if (reserve <= 0)
            {
                targetWeapon = w;
                break;
            }
        }

        if (targetWeapon == null)
        {
            Debug.Log($"[BotBuyWeapon] {gameObject.name} Tidak ada senjata dengan ammo habis. Credits={Credits}");
            return;
        }

        // Cari ammo yang cocok dan terjangkau.
        var affordable = availableAmmos
            .Where(a => a != null && a.IsCompatible(targetWeapon) && Credits >= a.Price)
            .OrderBy(a => a.Price)
            .ToList();

        if (affordable.Count == 0)
        {
            var compatible = availableAmmos.FirstOrDefault(a => a != null && a.IsCompatible(targetWeapon));
            if (compatible != null)
            {
                Debug.Log($"[BotBuyWeapon] {gameObject.name} Tidak cukup kredit untuk ammo {compatible.AmmoName} (harga {compatible.Price}). Credits={Credits}");
            }
            else
            {
                Debug.LogWarning($"[BotBuyWeapon] {gameObject.name} Tidak ada AmmoData yang cocok untuk {targetWeapon.WeaponName}. Cek availableAmmos/compatibleWeapons.");
            }
            return;
        }

        var ammo = affordable[0];

        // Kurangi kredit.
        Credits -= ammo.Price;

        // Tambah reserve untuk senjata target.
        if (!reserveAmmoByWeapon.ContainsKey(targetWeapon))
            reserveAmmoByWeapon[targetWeapon] = targetWeapon.StartingReserveAmmo;
        reserveAmmoByWeapon[targetWeapon] += ammo.AmountPerPurchase;

        // Jika senjata target sedang dipakai, tambahkan langsung ke controller.
        if (targetWeapon == CurrentWeapon && weaponController != null && weaponController.HasWeapon)
        {
            weaponController.AddAmmo(ammo.AmountPerPurchase);
        }

        Debug.Log($"[BotBuyWeapon] {gameObject.name} >>> BELI AMMO: {ammo.AmmoName} +{ammo.AmountPerPurchase} untuk {targetWeapon.WeaponName}. Sisa kredit {Credits}.");
    }

    /// <summary>Ganti ke senjata berikutnya dalam inventory.</summary>
    public void SwitchToNextWeapon()
    {
        if (ownedWeapons.Count == 0)
        {
            Debug.Log($"[BotBuyWeapon] {gameObject.name} Tidak punya senjata untuk diganti.");
            return;
        }

        if (ownedWeapons.Count == 1)
        {
            // Hanya punya satu senjata, pastikan ter-equip.
            if (currentWeaponIndex != 0)
            {
                currentWeaponIndex = 0;
                EquipOwnedWeapon(0);
            }
            return;
        }

        // Pindah ke senjata berikutnya (looping).
        int nextIndex = (currentWeaponIndex + 1) % ownedWeapons.Count;
        EquipOwnedWeapon(nextIndex);
    }

    private void EquipOwnedWeapon(int index)
    {
        if (index < 0 || index >= ownedWeapons.Count)
            return;

        // Simpan ammo senjata sebelumnya agar tidak hilang.
        if (CurrentWeapon != null && weaponController != null && weaponController.HasWeapon)
        {
            reserveAmmoByWeapon[CurrentWeapon] = weaponController.ReserveAmmo;
            currentAmmoByWeapon[CurrentWeapon] = weaponController.CurrentAmmo;
        }

        currentWeaponIndex = index;
        var weapon = ownedWeapons[index];

        if (!reserveAmmoByWeapon.ContainsKey(weapon))
            reserveAmmoByWeapon[weapon] = weapon.StartingReserveAmmo;
        if (!currentAmmoByWeapon.ContainsKey(weapon))
            currentAmmoByWeapon[weapon] = weapon.StartingMagazineAmmo;

        if (weaponController != null)
        {
            weaponController.ApplyWeaponData(weapon);
            weaponController.SetAmmo(currentAmmoByWeapon[weapon]);
            weaponController.SetReserveAmmo(reserveAmmoByWeapon[weapon]);
        }

        // Pastikan ammo tidak nol saat ganti senjata (hanya jika belum pernah dipakai).
        if (weaponController != null && weaponController.CurrentAmmo <= 0 && currentAmmoByWeapon[weapon] <= 0)
        {
            weaponController.SetAmmo(Mathf.Max(1, weapon.StartingMagazineAmmo));
            currentAmmoByWeapon[weapon] = weaponController.CurrentAmmo;
        }

        Debug.Log($"[BotBuyWeapon] {gameObject.name} Ganti senjata ke: {weapon.WeaponName} (index {index}). HasWeapon={weaponController?.HasWeapon}, Ammo={weaponController.CurrentAmmo}/{weaponController?.ReserveAmmo}");
    }

    /// <summary>Fallback beli ammo tanpa AmmoData (per senjata yang ammunya habis).</summary>
    private void TryBuyAmmoFallback()
    {
        // Sync reserve senjata aktif.
        if (CurrentWeapon != null && weaponController.HasWeapon)
            reserveAmmoByWeapon[CurrentWeapon] = weaponController.ReserveAmmo;

        WeaponData targetWeapon = null;
        foreach (var w in ownedWeapons)
        {
            int reserve = w.StartingReserveAmmo;
            if (reserveAmmoByWeapon.TryGetValue(w, out var r))
                reserve = r;
            if (w == CurrentWeapon && weaponController.HasWeapon)
                reserve = weaponController.ReserveAmmo;

            if (reserve <= 0)
            {
                targetWeapon = w;
                break;
            }
        }

        if (targetWeapon == null)
            return;

        if (Credits < fallbackAmmoPrice)
        {
            Debug.Log($"[BotBuyWeapon] {gameObject.name} (fallback) Tidak cukup kredit untuk ammo. Credits={Credits}");
            return;
        }

        Credits -= fallbackAmmoPrice;

        if (!reserveAmmoByWeapon.ContainsKey(targetWeapon))
            reserveAmmoByWeapon[targetWeapon] = targetWeapon.StartingReserveAmmo;
        reserveAmmoByWeapon[targetWeapon] += fallbackAmmoAmount;

        if (targetWeapon == CurrentWeapon && weaponController != null && weaponController.HasWeapon)
            weaponController.AddAmmo(fallbackAmmoAmount);

        Debug.Log($"[BotBuyWeapon] {gameObject.name} >>> (fallback) BELI AMMO +{fallbackAmmoAmount} untuk {targetWeapon.WeaponName}. Sisa kredit {Credits}.");
    }

    /// <summary>Reset inventory bot ke kondisi awal (untuk round baru).</summary>
    public void ResetInventory()
    {
        ownedWeapons.Clear();
        reserveAmmoByWeapon.Clear();
        currentAmmoByWeapon.Clear();
        currentWeaponIndex = -1;
        Credits = startingCredits;
        hasAttemptedInitialBuy = false;

        if (weaponController != null)
            weaponController.ClearWeapon();
    }

    /// <summary>Tambah kredit bot (opsional, misal setelah kill).</summary>
    public void AddCredits(int amount)
    {
        if (amount <= 0)
            return;

        Credits += amount;
        Debug.Log($"[BotBuyWeapon] {gameObject.name} Tambah kredit +{amount}. Total {Credits}");
    }
}
