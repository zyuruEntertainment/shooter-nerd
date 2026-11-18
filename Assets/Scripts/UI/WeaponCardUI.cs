using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Kartu item toko khusus WeaponData.
/// </summary>
public class WeaponCardUI : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private WeaponData weaponData;

    [Header("Premium (pembelian luar game)")]
    [Tooltip("Jika true, kartu hanya ditampilkan jika item sudah dibeli (status owned).")]
    [SerializeField] private bool isPremium = false;
    [Tooltip("Harga dalam cash untuk pembelian premium.")]
    [SerializeField, Min(0)] private int premiumPrice = 0;
    [Tooltip("ID unik item premium. Kosong = pakai nama weapon.")]
    [SerializeField] private string premiumId = string.Empty;

    [Header("Card UI")]
    [SerializeField] private Image cardPanel;
    [SerializeField] private Button cardButton;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.white;

    public WeaponData Weapon => weaponData;

    public bool IsPremium => isPremium;
    public int PremiumPrice => premiumPrice;
    public string PremiumId => string.IsNullOrWhiteSpace(premiumId) ? (weaponData != null ? weaponData.WeaponName : string.Empty) : premiumId;

    private UnityAction onSelected;

    private void Awake()
    {
        if (cardButton == null)
            cardButton = GetComponent<Button>();

        if (cardPanel != null)
            cardPanel.color = normalColor;
    }

    public void Initialize(
        WeaponData data,
        UnityAction selectionCallback)
    {
        weaponData = data;
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
}