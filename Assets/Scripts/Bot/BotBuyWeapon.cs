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

    [Header("Referensi")]
    [SerializeField] private ArcadeWeaponController weaponController;

    [Header("Pengaturan Beli")]
    [SerializeField] private float buyCheckInterval = 3f;
    [SerializeField] private bool canBuyWeapons = true;
    [SerializeField] private bool buyImmediatelyOnStart = true;

    [Header("Pengaturan Ganti Senjata")]
    [SerializeField] private float switchWeaponInterval = 10f;
    [SerializeField] private bool canSwitchWeapons = true;

    private readonly List<WeaponData> ownedWeapons = new List<WeaponData>();
    private float buyTimer;
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

        Debug.Log($"[BotBuyWeapon] {gameObject.name} Awake. weaponController={weaponController != null}, availableWeapons={availableWeapons?.Length ?? 0}");

        // Mulai dengan inventory kosong dan kredit 2500 setiap kali Play.
        ownedWeapons.Clear();
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

            // Kurangi kredit.
            Credits -= weapon.Price;

            // Tambah ke inventory.
            ownedWeapons.Add(weapon);

            // Terapkan ke ArcadeWeaponController (sama seperti player equip).
            weaponController.ApplyWeaponData(weapon);
            currentWeaponIndex = ownedWeapons.IndexOf(weapon);

            // Pastikan ammo tidak nol agar bot bisa menembak segera.
            if (weaponController != null && weaponController.CurrentAmmo <= 0)
            {
                weaponController.SetAmmo(1);
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

        currentWeaponIndex = index;
        var weapon = ownedWeapons[index];

        if (weaponController != null)
        {
            weaponController.ApplyWeaponData(weapon);
        }

        // Pastikan ammo tidak nol saat ganti senjata.
        if (weaponController != null && weaponController.CurrentAmmo <= 0)
        {
            weaponController.SetAmmo(1);
        }

        Debug.Log($"[BotBuyWeapon] {gameObject.name} Ganti senjata ke: {weapon.WeaponName} (index {index}). HasWeapon={weaponController?.HasWeapon}, Ammo={weaponController.CurrentAmmo}");
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
