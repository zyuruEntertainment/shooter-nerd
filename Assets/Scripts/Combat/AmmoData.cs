using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data ammo yang bisa ditetapkan ke banyak senjata.
/// Satu AmmoData dapat digunakan oleh beberapa WeaponData melalui daftar weapons yang cocok.
/// Buat lewat menu: Create > Shooter Nerd > Ammo Data.
/// </summary>
[CreateAssetMenu(fileName = "AmmoData", menuName = "Shooter Nerd/Ammo Data", order = 2)]
public class AmmoData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string ammoName = "New Ammo";
    [SerializeField] private Sprite icon;

    [Header("Harga & Jumlah")]
    [SerializeField, Min(0)] private int price = 100;
    [SerializeField, Min(1)] private int amountPerPurchase = 30;

    [Header("Senjata yang menggunakan ammo ini")]
    [Tooltip("Daftar senjata yang menggunakan tipe ammo ini. Kosongkan jika tidak ingin membatasi.")]
    [SerializeField] private List<WeaponData> compatibleWeapons = new List<WeaponData>();

    public string AmmoName => ammoName;
    public Sprite Icon => icon;
    public int Price => price;
    public int AmountPerPurchase => amountPerPurchase;
    public IReadOnlyList<WeaponData> CompatibleWeapons => compatibleWeapons;

    /// <summary>Cek apakah weapon ini cocok dengan ammo ini.</summary>
    public bool IsCompatible(WeaponData weapon)
    {
        if (weapon == null)
            return false;

        // Jika daftar kosong, anggap semua weapon cocok.
        if (compatibleWeapons == null || compatibleWeapons.Count == 0)
            return true;

        return compatibleWeapons.Contains(weapon);
    }
}
