using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pengatur toko tunggal (single source of truth).
/// Tangani seleksi kartu item, panel informasi, tampilan saldo, dan transaksi pembelian.
/// UI tidak mengelola saldo atau kepemilikan sendiri; keduanya ada di PlayerWallet dan WeaponManager.
/// </summary>
public class WeaponShopController : MonoBehaviour
{
    public static WeaponShopController Instance { get; private set; }

    [Header("Katalog Toko")]
    [SerializeField] private WeaponData[] weapons;

    [Header("Referensi UI")]
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private TMP_Text statusText;

    [Header("Referensi Logika")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private PlayerWallet wallet;

    private WeaponShopItemUI itemPanel;
    private Button buyButton;
    private readonly List<WeaponCardUI> cards = new List<WeaponCardUI>();
    private WeaponCardUI selectedCard;
    private WeaponData selectedWeapon;
    private bool isInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (weaponManager == null)
            weaponManager = WeaponManager.Instance;
        if (wallet == null)
            wallet = PlayerWallet.Instance;

        itemPanel = GetComponent<WeaponShopItemUI>();

        // Pastikan ada tombol beli/equip terpisah (tidak bergantung pada kartu item di prefab).
        EnsureBuyButton();
    }

    private void Start()
    {
        if (isInitialized)
            return;
        isInitialized = true;

        if (weaponManager == null || wallet == null || itemPanel == null)
        {
            Debug.LogError("WeaponShopController: WeaponManager, PlayerWallet, atau WeaponShopItemUI (panel info) belum tersedia.", this);
            return;
        }

        BuildCards();
        wallet.OnCreditsChanged += UpdateCredits;
        UpdateCredits(wallet.Credits);

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        // Pilih item pertama agar panel informasi langsung terlihat saat toko dibuka.
        SelectFirst();
    }

    private void OnDestroy()
    {
        if (wallet != null)
            wallet.OnCreditsChanged -= UpdateCredits;

        if (buyButton != null)
            buyButton.onClick.RemoveAllListeners();

        if (Instance == this)
            Instance = null;
    }

    private void EnsureBuyButton()
    {
        if (buyButton != null)
            return;

        var existing = transform.Find("ShopBuyButton")?.GetComponent<Button>();
        if (existing != null)
        {
            buyButton = existing;
            return;
        }

        // Buat tombol beli/equip di root shop agar tidak bentrok dengan kartu item (WeaponCardUI).
        var go = new GameObject("ShopBuyButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200, 50);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        buyButton = go.GetComponent<Button>();
        buyButton.targetGraphic = img;
    }

    private void BuildCards()
    {
        WeaponCardUI[] found = GetComponentsInChildren<WeaponCardUI>(true);
        int index = 0;

        foreach (WeaponCardUI card in found)
        {
            if (card == null)
                continue;

            if (index < weapons.Length && weapons[index] != null)
            {
                card.gameObject.SetActive(true);
                card.Initialize(weapons[index], () => OnCardSelected(card));
                cards.Add(card);
                index++;
            }
            else
            {
                card.gameObject.SetActive(false);
            }
        }
    }

    private void SelectFirst()
    {
        if (cards.Count > 0)
            OnCardSelected(cards[0]);
    }

    private void OnCardSelected(WeaponCardUI card)
    {
        if (card == null || card.Weapon == null)
            return;

        // Hanya satu kartu yang boleh terpilih: yang dipilih putih, sisanya normal.
        foreach (WeaponCardUI c in cards)
        {
            if (c != null)
                c.SetSelected(c == card);
        }

        selectedCard = card;
        selectedWeapon = card.Weapon;
        RefreshInfoPanel();
    }

    private void RefreshInfoPanel()
    {
        if (itemPanel == null || selectedWeapon == null)
            return;

        bool owned = weaponManager.IsOwned(selectedWeapon);
        bool equipped = weaponManager.IsEquipped(selectedWeapon);

        // Panel informasi menampilkan item yang dipilih terakhir: nama, ikon, harga, status kepemilikan, dll.
        itemPanel.Setup(selectedWeapon, owned, equipped, OnBuyClicked);

        // Tombol beli/equip tidak dapat diklik jika item sudah dipakai (status EQUIPPED).
        if (buyButton != null)
            buyButton.interactable = !equipped;
    }

    public void OnBuyClicked()
    {
        if (selectedWeapon == null || weaponManager == null)
            return;

        bool success = weaponManager.TryPurchaseOrEquip(selectedWeapon, out string message);

        if (statusText != null)
            statusText.text = message;

        if (success)
        {
            // Perbarui panel informasi dan status kartu (miliki/equip) setelah transaksi.
            RefreshInfoPanel();
        }

        UpdateCredits(wallet.Credits);
    }

    private void UpdateCredits(int amount)
    {
        if (creditsText != null)
            creditsText.text = amount.ToString("N0");
    }

    public void Refresh()
    {
        RefreshInfoPanel();
        UpdateCredits(wallet != null ? wallet.Credits : 0);
    }
}
