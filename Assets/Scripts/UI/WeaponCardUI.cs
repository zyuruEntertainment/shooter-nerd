using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Kartu item toko yang mendukung baik WeaponData maupun AmmoData.
/// Jika weaponData diisi, kartu menampilkan weapon. Jika ammoData diisi, kartu menampilkan ammo.
/// </summary>
public class WeaponCardUI : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private AmmoData ammoData;

    [Header("Pengaturan Kartu")]
    [Tooltip("Centang jika kartu ini khusus untuk menampilkan ammo (bukan weapon).")]
    [SerializeField] private bool isAmmoCard = false;

    [Header("Card UI")]
    [SerializeField] private Image cardPanel;
    [SerializeField] private Button cardButton;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.white;

    public WeaponData Weapon => weaponData;
    public AmmoData Ammo => ammoData;

    /// <summary>Apakah kartu ini menampilkan ammo (bukan weapon).</summary>
    public bool IsAmmo => ammoData != null || isAmmoCard;

    private UnityAction onSelected;

    private void Awake()
    {
        if (cardButton == null)
            cardButton = GetComponent<Button>();

        if (cardPanel != null)
            cardPanel.color = normalColor;
    }

    /// <summary>Inisialisasi kartu untuk weapon.</summary>
    public void Initialize(
        WeaponData data,
        UnityAction selectionCallback)
    {
        weaponData = data;
        ammoData = null;
        isAmmoCard = false;
        onSelected = selectionCallback;

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(Select);
        }
    }

    /// <summary>Inisialisasi kartu untuk ammo.</summary>
    public void Initialize(
        AmmoData data,
        UnityAction selectionCallback)
    {
        ammoData = data;
        weaponData = null;
        isAmmoCard = true;
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
