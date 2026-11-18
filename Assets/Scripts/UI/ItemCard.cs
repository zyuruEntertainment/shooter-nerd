using UnityEngine;

/// <summary>
/// Penghubung (connector) untuk pembelian luar game.
/// Menghubungkan sebuah kartu (WeaponCardUI/AmmoCardUI) dengan ShopItemAsset,
/// dan menerapkan data (premium, harga, nama) dari asset ke kartu.
/// </summary>
public class ItemCard : MonoBehaviour
{
    [Header("Koneksi")]
    [Tooltip("Kartu yang dihubungkan (WeaponCardUI atau AmmoCardUI).")]
    [SerializeField] private Component card;

    [Tooltip("Asset toko yang menyediakan harga, status premium, dan nama item.")]
    [SerializeField] private ShopItemAsset shopItemAsset;

    [Header("Colors (seperti Weapon/Ammo Card)")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.white;

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    /// <summary>Terapkan data dari ShopItemAsset ke kartu yang dihubungkan.</summary>
    public void Apply()
    {
        if (card == null || shopItemAsset == null)
            return;

        if (card is WeaponCardUI weaponCard)
        {
            weaponCard.ApplyShopItem(shopItemAsset);
            weaponCard.SetColors(normalColor, selectedColor);
        }
        else if (card is AmmoCardUI ammoCard)
        {
            ammoCard.ApplyShopItem(shopItemAsset);
            ammoCard.SetColors(normalColor, selectedColor);
        }

        // Nama juga ikut dari asset.
        if (!string.IsNullOrWhiteSpace(shopItemAsset.ItemName))
            gameObject.name = shopItemAsset.ItemName;
    }

    /// <summary>Beli item ini (premium) menggunakan OutGamePurchaseController.</summary>
    public bool Purchase()
    {
        if (shopItemAsset == null)
            return false;

        if (OutGamePurchaseController.Instance == null)
            return false;

        if (card is WeaponCardUI weaponCard)
            return OutGamePurchaseController.Instance.Purchase(weaponCard);
        if (card is AmmoCardUI ammoCard)
            return OutGamePurchaseController.Instance.Purchase(ammoCard);

        return OutGamePurchaseController.Instance.PurchaseItem(shopItemAsset.PremiumId, shopItemAsset.Price);
    }
}