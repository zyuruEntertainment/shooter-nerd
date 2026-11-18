using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aset toko yang dapat dibuat lewat menu Create > Shooter Nerd > Shop Item.
/// Berisi harga, tipe mata uang, data weapon/ammo, flag premium, dan daftar bundle.
/// </summary>
[CreateAssetMenu(fileName = "ShopItem", menuName = "Shooter Nerd/Shop Item", order = 4)]
public class ShopItemAsset : ScriptableObject
{
    public enum CurrencyType { Credits, Cash }

    [Header("Identity")]
    [SerializeField] private string itemName = "New Item";
    [SerializeField] private Sprite icon;

    [Header("Harga")]
    [SerializeField, Min(0)] private int price = 0;
    [SerializeField] private CurrencyType currency = CurrencyType.Credits;

    [Header("Premium (luar game)")]
    [SerializeField] private bool isPremium = false;

    [Header("Item (isi salah satu)")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private AmmoData ammoData;

    [Header("Bundle (gabungan beberapa item)")]
    [Tooltip("Daftar ID premium item yang termasuk dalam bundle ini.")]
    [SerializeField] private List<string> bundleItemIds = new List<string>();

    public string ItemName => itemName;
    public Sprite Icon => icon;
    public int Price => price;
    public CurrencyType Currency => currency;
    public bool IsPremium => isPremium;
    public WeaponData WeaponData => weaponData;
    public AmmoData AmmoData => ammoData;
    public IReadOnlyList<string> BundleItemIds => bundleItemIds;

    /// <summary>Apakah aset ini berisi weapon (bukan ammo).</summary>
    public bool IsWeapon => weaponData != null && ammoData == null;

    /// <summary>Apakah aset ini berisi ammo (bukan weapon).</summary>
    public bool IsAmmo => ammoData != null && weaponData == null;

    /// <summary>ID unik item (untuk kepemilikan premium). Kosong jika tidak premium.</summary>
    public string PremiumId => isPremium ? (weaponData != null ? weaponData.WeaponName : (ammoData != null ? ammoData.AmmoName : itemName)) : string.Empty;
}