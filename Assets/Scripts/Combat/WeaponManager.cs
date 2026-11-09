using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("Weapon Setup")]
    [SerializeField] private ArcadeWeaponController weaponController;
    [SerializeField] private WeaponData startingWeapon;
    [SerializeField] private List<WeaponData> startingOwnedWeapons = new List<WeaponData>();

    private readonly List<WeaponData> ownedWeapons = new List<WeaponData>();

    public WeaponData EquippedWeapon { get; private set; }
    public IReadOnlyList<WeaponData> OwnedWeapons => ownedWeapons;

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

        foreach (WeaponData weapon in startingOwnedWeapons)
            AddOwnedWeapon(weapon);

        if (startingWeapon != null)
        {
            AddOwnedWeapon(startingWeapon);
            Equip(startingWeapon);
        }
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
            Debug.LogError($"WeaponManager: ArcadeWeaponController tidak ditemukan di {gameObject.name}. Tidali dapat equip weapon {weapon?.WeaponName}.");
            return false;
        }

        // Reset fire timer when switching weapons to avoid immediate fire on equip
        if (weaponController is ArcadeWeaponController awc)
        {
            awc.ResetFireTimer();
        }

        EquippedWeapon = weapon;
        weaponController.ApplyWeaponData(weapon);
        Debug.Log($"WeaponManager: {weapon.WeaponName} berhasil di-equip.");
        return true;
    }

    private void AddOwnedWeapon(WeaponData weapon)
    {
        if (weapon != null && !ownedWeapons.Contains(weapon))
            ownedWeapons.Add(weapon);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
