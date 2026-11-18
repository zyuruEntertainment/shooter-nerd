using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Kartu item toko khusus AmmoData.
/// </summary>
public class AmmoCardUI : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private AmmoData ammoData;

    [Header("Premium (pembelian luar game)")]
    [Tooltip("Jika true, kartu hanya ditampilkan jika item sudah dibeli (status owned).")]
    [SerializeField] private bool isPremium = false;
    [Tooltip("Harga dalam cash untuk pembelian premium.")]
    [SerializeField, Min(0)] private int premiumPrice = 0;
    [Tooltip("ID unik item premium. Kosong = pakai nama ammo.")]
    [SerializeField] private string premiumId = string.Empty;

    [Header("Card UI")]
    [SerializeField] private Image cardPanel;
    [SerializeField] private Button cardButton;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.white;

    public AmmoData Ammo => ammoData;

    public bool IsPremium => isPremium;
    public int PremiumPrice => premiumPrice;
    public string PremiumId => string.IsNullOrWhiteSpace(premiumId) ? (ammoData != null ? ammoData.AmmoName : string.Empty) : premiumId;

    /// <summary>Terapkan data dari ShopItemAsset (premium, harga, id, nama) ke kartu.</summary>
    public void ApplyShopItem(ShopItemAsset asset)
    {
        if (asset == null)
            return;

        isPremium = asset.IsPremium;
        premiumPrice = asset.Price;
        if (string.IsNullOrWhiteSpace(premiumId))
            premiumId = asset.PremiumId;

        if (asset.AmmoData != null)
            ammoData = asset.AmmoData;
    }

    private UnityAction onSelected;

    private void Awake()
    {
        if (cardButton == null)
            cardButton = GetComponent<Button>();

        if (cardPanel != null)
            cardPanel.color = normalColor;
    }

    public void Initialize(
        AmmoData data,
        UnityAction selectionCallback)
    {
        ammoData = data;
        onSelected = selectionCallback;

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(Select);
        }
    }

    public void Select()
    {
        onSelected?.Invoke();
    }

    public void SetSelected(bool selected)
    {
        if (cardPanel == null)
            return;

        cardPanel.color = selected
            ? selectedColor
            : normalColor;
    }

    /// <summary>Atur warna kartu (normal/selected) dari luar (mis. ItemCard).</summary>
    public void SetColors(Color normal, Color selected)
    {
        normalColor = normal;
        selectedColor = selected;
        if (cardPanel != null)
            cardPanel.color = normalColor;
    }
}