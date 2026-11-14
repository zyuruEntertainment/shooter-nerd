using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("Weapon Setup")]
    [SerializeField] private ArcadeWeaponController weaponController;
    [SerializeField] private WeaponData startingWeapon;
    [SerializeField] private List<WeaponData> startingOwnedWeapons = new List<WeaponData>();

    [Header("Catalog (digunakan untuk rekonstruksi penyimpanan)")]
    [SerializeField] private WeaponData[] allWeapons = new WeaponData[0];

    private readonly List<WeaponData> ownedWeapons = new List<WeaponData>();

    // Reserve ammo per senjata agar tidak saling berbagi antar senjata.
    private readonly Dictionary<WeaponData, int> reserveAmmoByWeapon = new Dictionary<WeaponData, int>();

    // Current (magazine) ammo per senjata agar tidak reset saat ganti senjata.
    private readonly Dictionary<WeaponData, int> currentAmmoByWeapon = new Dictionary<WeaponData, int>();

    public WeaponData EquippedWeapon { get; private set; }
    public IReadOnlyList<WeaponData> OwnedWeapons => ownedWeapons;

    /// <summary>ArcadeWeaponController yang aktif dan sedang digunakan oleh pemain.</summary>
    public ArcadeWeaponController ActiveWeaponController => weaponController;

    /// <summary>Dipanggil setiap kali daftar kepemilihan atau senjata aktif berubah.</summary>
    public event Action OnOwnershipChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (weaponController == null)
            weaponController = GetComponent<ArcadeWeaponController>();

        // Mulai setiap kali Play dengan inventory senjata kosong:
        // tidak memuat senjata dari penyimpanan, tidak menggunakan
        // startingOwnedWeapons, dan tidak melengkapi startingWeapon.
        ownedWeapons.Clear();
        EquippedWeapon = null;

        // Pastikan semua weapon controller pada Player tidak menampilkan senjata default
        // saat inventory kosong. (Terkadang ada duplikat ArcadeWeaponController di prefab.)
        var controllers = GetComponents<ArcadeWeaponController>();
        foreach (var ctrl in controllers)
        {
            ctrl.ClearWeapon();
        }

        // Buat berkas penyimpanan agar state awal (kosong) tersimpan dan konsisten.
        SyncAndSaveOwnership();
    }

    public bool IsOwned(WeaponData weapon)
    {
        return weapon != null && ownedWeapons.Contains(weapon);
    }

    public bool IsEquipped(WeaponData weapon)
    {
        return weapon != null && EquippedWeapon == weapon;
    }

    public bool TryPurchaseOrEquip(WeaponData weapon, out string message)
    {
        message = string.Empty;

        if (weapon == null)
        {
            message = "Weapon data belum diatur.";
            return false;
        }

        if (IsOwned(weapon))
        {
            bool equipped = Equip(weapon);
            message = equipped ? weapon.WeaponName + " equipped." : "Weapon controller tidak ditemukan.";
            return equipped;
        }

        if (PlayerWallet.Instance == null)
        {
            message = "PlayerWallet belum dipasang di scene Gameplay.";
            return false;
        }

        if (!PlayerWallet.Instance.TrySpend(weapon.Price))
        {
            message = "Credits tidak cukup.";
            return false;
        }

        AddOwnedWeapon(weapon);

        if (!Equip(weapon))
        {
            // Refund jika controller tidak tersedia,agar uang pemain tidak hilang.
            PlayerWallet.Instance.AddCredits(weapon.Price);
            ownedWeapons.Remove(weapon);
            SyncAndSaveOwnership();
            message = "Weapon controller tidak ditemukan - pastikan ArcadeWeaponController ada di scene.";
            return false;
        }

        message = weapon.WeaponName + " berhasil dibeli dan dipakai.";
        return true;
    }

    public bool Equip(WeaponData weapon)
    {
        if (!IsOwned(weapon))
            return false;

        // Coba dapatkan komponen jika belum diassign
        if (weaponController == null)
            weaponController = GetComponent<ArcadeWeaponController>();

        if (weaponController == null)
        {
            Debug.LogError($"WeaponManager: ArcadeWeaponController tidak ditemukan di {gameObject.name}. Tidak dapat equip weapon {weapon?.WeaponName}.");
            return false;
        }

        // Simpan state ammo senjata sebelumnya agar tidak hilang/reset saat ganti senjata.
        if (EquippedWeapon != null && EquippedWeapon != weapon && weaponController.HasWeapon)
        {
            reserveAmmoByWeapon[EquippedWeapon] = weaponController.ReserveAmmo;
            currentAmmoByWeapon[EquippedWeapon] = weaponController.CurrentAmmo;
        }

        // Reset fire timer when switching weapons to avoid immediate fire on equip
        if (weaponController is ArcadeWeaponController awc)
        {
            awc.ResetFireTimer();
        }

        EquippedWeapon = weapon;

        // Simpan state awal jika belum ada.
        if (!reserveAmmoByWeapon.ContainsKey(weapon))
            reserveAmmoByWeapon[weapon] = weapon.StartingReserveAmmo;
        if (!currentAmmoByWeapon.ContainsKey(weapon))
            currentAmmoByWeapon[weapon] = weapon.StartingMagazineAmmo;

        weaponController.ApplyWeaponData(weapon);

        // Terapkan ammo (magazine + reserve) khusus senjata ini.
        weaponController.SetAmmo(currentAmmoByWeapon[weapon]);
        weaponController.SetReserveAmmo(reserveAmmoByWeapon[weapon]);

        SyncAndSaveOwnership();
        Debug.Log($"WeaponManager: {weapon.WeaponName} berhasil di-equip.");
        return true;
    }

    private void AddOwnedWeapon(WeaponData weapon)
    {
        if (weapon != null && !ownedWeapons.Contains(weapon))
        {
            ownedWeapons.Add(weapon);
            if (!reserveAmmoByWeapon.ContainsKey(weapon))
                reserveAmmoByWeapon[weapon] = weapon.StartingReserveAmmo;
            if (!currentAmmoByWeapon.ContainsKey(weapon))
                currentAmmoByWeapon[weapon] = weapon.StartingMagazineAmmo;
            OnOwnershipChanged?.Invoke();
            SyncAndSaveOwnership();
        }
    }

    private WeaponData FindWeaponByName(string name)
    {
        if (allWeapons != null)
        {
            foreach (WeaponData weapon in allWeapons)
            {
                if (weapon != null && weapon.WeaponName == name)
                    return weapon;
            }
        }

        foreach (WeaponData weapon in ownedWeapons)
        {
            if (weapon != null && weapon.WeaponName == name)
                return weapon;
        }

        return null;
    }

    private void SyncAndSaveOwnership()
    {
        SaveSystem.currentData.ownedWeaponNames = ownedWeapons
            .Where(w => w != null)
            .Select(w => w.WeaponName)
            .ToList();
        SaveSystem.currentData.equippedWeaponName = EquippedWeapon != null ? EquippedWeapon.WeaponName : string.Empty;
        SaveSystem.Save();
    }

    /// <summary>Tambah reserve ammo untuk senjata tertentu (dan sync ke controller jika sedang equipped).</summary>
    public void AddReserveAmmo(WeaponData weapon, int amount)
    {
        if (weapon == null || amount <= 0)
            return;

        if (!reserveAmmoByWeapon.ContainsKey(weapon))
            reserveAmmoByWeapon[weapon] = weapon.StartingReserveAmmo;

        reserveAmmoByWeapon[weapon] += amount;

        if (EquippedWeapon == weapon && weaponController != null)
            weaponController.SetReserveAmmo(reserveAmmoByWeapon[weapon]);
    }

    /// <summary>Set reserve ammo untuk senjata tertentu (digunakan saat reload).</summary>
    public void SetReserveAmmo(WeaponData weapon, int amount)
    {
        if (weapon == null)
            return;

        reserveAmmoByWeapon[weapon] = Mathf.Max(0, amount);

        if (EquippedWeapon == weapon && weaponController != null)
            weaponController.SetReserveAmmo(reserveAmmoByWeapon[weapon]);
    }

    /// <summary>Reset inventory senjata ke kondisi awal (untuk round baru).</summary>
    public void ResetInventory()
    {
        ownedWeapons.Clear();
        reserveAmmoByWeapon.Clear();
        currentAmmoByWeapon.Clear();
        EquippedWeapon = null;

        if (weaponController != null)
            weaponController.ClearWeapon();

        var controllers = GetComponents<ArcadeWeaponController>();
        foreach (var ctrl in controllers)
            ctrl.ClearWeapon();

        SyncAndSaveOwnership();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
