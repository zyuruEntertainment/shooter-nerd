using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pengatur toko tunggal (single source of truth).
/// Tangani seleksi kartu item, panel informasi, tampilan saldo, dan transaksi pembelian weapon maupun ammo.
/// UI tidak mengelola saldo atau kepemilikan sendiri; keduanya ada di PlayerWallet dan WeaponManager.
/// </summary>
public class WeaponShopController : MonoBehaviour
{
    public static WeaponShopController Instance { get; private set; }

    [Header("Katalog Toko - Weapon")]
    [SerializeField] private WeaponData[] weapons;

    [Header("Katalog Toko - Ammo")]
    [SerializeField] private AmmoData[] ammos;

    [Header("Referensi UI")]
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text ammoStatusText; // menampilkan ammo aktif (x/x)

    [Header("Referensi Logika")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private ArcadeWeaponController playerWeaponController;

    [Header("Mode Toko")]
    [SerializeField] private bool showAmmoCatalog = true;

    private WeaponShopItemUI itemPanel;
    private Button buyButton;
    private readonly List<WeaponCardUI> weaponCards = new List<WeaponCardUI>();
    private readonly List<AmmoCardUI> ammoCards = new List<AmmoCardUI>();
    private WeaponData selectedWeapon;
    private AmmoData selectedAmmo;
    private bool isInitialized;
    private bool isBuyingAmmo;

    private void OnEnable()
    {
        PremiumOwnership.OnOwnershipChanged += HandlePremiumOwnershipChanged;
    }

    private void OnDisable()
    {
        PremiumOwnership.OnOwnershipChanged -= HandlePremiumOwnershipChanged;
    }

    private void HandlePremiumOwnershipChanged()
    {
        if (!isInitialized)
            return;
        RebuildCards();
    }

    private void RebuildCards()
    {
        weaponCards.Clear();
        ammoCards.Clear();
        BuildCards();
        BuildAmmoCards();
    }

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

        // SELALU ambil ArcadeWeaponController dari WeaponManager (yang aktif dan digunakan pemain),
        // bukan mencari sendiri via tag yang bisa dapat controller yang salah.
        if (WeaponManager.Instance != null && WeaponManager.Instance.ActiveWeaponController != null)
        {
            playerWeaponController = WeaponManager.Instance.ActiveWeaponController;
        }
        else
        {
            Debug.LogWarning("[WeaponShopController] WeaponManager.Instance atau ActiveWeaponController null di Awake!");
        }

        itemPanel = GetComponent<WeaponShopItemUI>();

        // Pastikan ada tombol beli/equip terpisah (tidak bergantung pada kartu item di prefab).
        EnsureBuyButton();
    }

    private void Start()
    {
        if (isInitialized)
            return;
        isInitialized = true;

        // Pastikan selalu terhubung ke instance aktif di scene via singleton,
        // agar tidak bergantung pada referensi prefab yang bisa tidak terresolve
        // saat BuySystem di-instantiate di runtime.
        weaponManager = WeaponManager.Instance;
        wallet = PlayerWallet.Instance;

        // SELALU ambil playerWeaponController dari WeaponManager (controller aktif pemain).
        // Jangan pakai referensi Inspector yang bisa menunjuk ke controller yang salah.
        if (WeaponManager.Instance != null && WeaponManager.Instance.ActiveWeaponController != null)
        {
            playerWeaponController = WeaponManager.Instance.ActiveWeaponController;
        }
        else
        {
            Debug.LogWarning("[WeaponShopController] WeaponManager.Instance atau ActiveWeaponController null!");
        }

        if (weaponManager == null || wallet == null || itemPanel == null)
        {
            Debug.LogError("WeaponShopController: WeaponManager, PlayerWallet, atau WeaponShopItemUI (panel info) belum tersedia.", this);
            return;
        }

        if (statusText == null)
        {
            Debug.LogError("WeaponShopController: statusText belum diatur di prefab.", this);
        }

        BuildCards();
        BuildAmmoCards();
        wallet.OnCreditsChanged += UpdateCredits;
        UpdateCredits(wallet.Credits);
        UpdateAmmoStatus();

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

        // Cari tombol beli yang sudah ada di prefab (nama "BuyButton"), cari rekursif di seluruh children.
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.name == "BuyButton")
            {
                buyButton = btn;
                return;
            }
        }

        // Fallback: cari nama alternatif.
        foreach (var btn in buttons)
        {
            if (btn.name == "ShopBuyButton")
            {
                buyButton = btn;
                return;
            }
        }

        // Tidak membuat pengganti: prefab wajib menyediakan tombol beli.
        Debug.LogError("WeaponShopController: tombol beli (BuyButton/ShopBuyButton) tidak ditemukan di prefab. Pastikan referensi prefab benar.", this);
    }

    private void BuildCards()
    {
        WeaponCardUI[] found = GetComponentsInChildren<WeaponCardUI>(true);

        foreach (WeaponCardUI card in found)
        {
            if (card == null)
                continue;

            // Data diambil dari konfigurasi prefab, bukan dari urutan child.
            if (card.Weapon != null)
            {
                // Item premium yang belum dibeli tidak ditampilkan.
                if (card.IsPremium && !PremiumOwnership.IsOwned(card.PremiumId))
                {
                    card.gameObject.SetActive(false);
                    continue;
                }

                card.gameObject.SetActive(true);
                card.Initialize(card.Weapon, () => OnCardSelected(card));
                weaponCards.Add(card);
            }
            else
            {
                card.gameObject.SetActive(false);
            }
        }
    }

    private void BuildAmmoCards()
    {
        AmmoCardUI[] found = GetComponentsInChildren<AmmoCardUI>(true);

        foreach (AmmoCardUI card in found)
        {
            if (card == null)
                continue;

            // Data diambil dari konfigurasi prefab, bukan dari urutan child.
            if (card.Ammo != null)
            {
                // Item premium yang belum dibeli tidak ditampilkan.
                if (card.IsPremium && !PremiumOwnership.IsOwned(card.PremiumId))
                {
                    card.gameObject.SetActive(false);
                    continue;
                }

                card.gameObject.SetActive(true);
                card.Initialize(card.Ammo, () => OnAmmoCardSelected(card));
                ammoCards.Add(card);
            }
            else
            {
                card.gameObject.SetActive(false);
            }
        }
    }

    private void SelectFirst()
    {
        if (weaponCards.Count > 0)
            OnCardSelected(weaponCards[0]);
        else if (showAmmoCatalog && ammoCards.Count > 0)
        {
            // Tidak ada weapon card, pilih ammo pertama.
            OnAmmoCardSelected(ammoCards[0]);
        }
    }

    /// <summary>Pilih weapon card (dari UI).</summary>
    private void OnCardSelected(WeaponCardUI card)
    {
        if (card == null || card.Weapon == null)
            return;

        isBuyingAmmo = false;
        selectedAmmo = null;

        // Hanya satu kartu yang boleh terpilih: yang dipilih putih, sisanya normal.
        foreach (WeaponCardUI c in weaponCards)
        {
            if (c != null)
                c.SetSelected(c == card);
        }
        foreach (AmmoCardUI c in ammoCards)
        {
            if (c != null)
                c.SetSelected(false);
        }
        selectedWeapon = card.Weapon;
        RefreshInfoPanel();
    }

    /// <summary>Pilih ammo card (dari UI).</summary>
    private void OnAmmoCardSelected(AmmoCardUI card)
    {
        if (card == null || card.Ammo == null)
            return;

        isBuyingAmmo = true;
        selectedWeapon = null;
        selectedAmmo = card.Ammo;

        // Kosongkan seleksi weapon card.
        foreach (WeaponCardUI c in weaponCards)
        {
            if (c != null)
                c.SetSelected(false);
        }
        foreach (AmmoCardUI c in ammoCards)
        {
            if (c != null)
                c.SetSelected(c == card);
        }

        RefreshInfoPanel();
    }

    private void RefreshInfoPanel()
    {
        if (itemPanel == null)
            return;

        UpdateAmmoStatus();

        if (isBuyingAmmo && selectedAmmo != null)
        {
            // Tampilkan info ammo.
            bool canAfford = wallet != null && wallet.CanAfford(selectedAmmo.Price);
            itemPanel.SetupAmmo(selectedAmmo, canAfford, OnBuyAmmoClicked);

            if (buyButton != null)
                buyButton.interactable = canAfford;
        }
        else if (selectedWeapon != null)
        {
            // Tampilkan info weapon.
            bool owned = weaponManager.IsOwned(selectedWeapon);
            bool equipped = weaponManager.IsEquipped(selectedWeapon);

            itemPanel.Setup(selectedWeapon, owned, equipped, OnBuyClicked);

            // Tombol beli/equip tidak dapat diklik jika item sudah dipakai (status EQUIPPED).
            if (buyButton != null)
                buyButton.interactable = !equipped;
        }
    }

    public void OnBuyClicked()
    {
        if (isBuyingAmmo)
        {
            OnBuyAmmoClicked();
            return;
        }

        if (selectedWeapon == null || weaponManager == null)
            return;

        if (wallet == null)
            wallet = PlayerWallet.Instance;

        bool success = weaponManager.TryPurchaseOrEquip(selectedWeapon, out string message);

        if (statusText != null)
            statusText.text = message;

        if (success)
        {
            RefreshInfoPanel();
        }

        UpdateCredits(wallet.Credits);
    }

    private void OnBuyAmmoClicked()
    {
        if (selectedAmmo == null || wallet == null || playerWeaponController == null || weaponManager == null)
            return;

        if (!wallet.CanAfford(selectedAmmo.Price))
        {
            if (statusText != null)
                statusText.text = "Credits tidak cukup untuk beli ammo.";
            return;
        }

        // Cek apakah senjata yang sedang dipakai (equipped) cocok dengan ammo ini.
        WeaponData equipped = weaponManager.EquippedWeapon;
        if (equipped == null || !selectedAmmo.IsCompatible(equipped))
        {
            if (statusText != null)
                statusText.text = "Senjata aktif tidak cocok dengan ammo ini.";
            Debug.LogWarning($"[WeaponShopController] Senjata equipped ({equipped?.WeaponName ?? "none"}) tidak cocok dengan ammo {selectedAmmo.AmmoName}");
            return;
        }

        // Kurangi kredit hanya jika transaksi berhasil.
        if (!wallet.TrySpend(selectedAmmo.Price))
        {
            if (statusText != null)
                statusText.text = "Credits tidak cukup untuk beli ammo.";
            return;
        }

        // Tambah ammo ke senjata yang sedang dipakai (persisten per senjata).
        int reserveBefore = playerWeaponController != null ? playerWeaponController.ReserveAmmo : 0;
        if (weaponManager != null)
        {
            weaponManager.AddReserveAmmo(equipped, selectedAmmo.AmountPerPurchase);
            // Pastikan controller yang aktif (UI) ikut tersinkron (hindari desync rifle/sniper).
            if (playerWeaponController != null && weaponManager.EquippedWeapon == equipped)
                playerWeaponController.SetReserveAmmo(weaponManager.GetReserveAmmo(equipped));
        }
        else if (playerWeaponController != null)
            playerWeaponController.AddAmmo(selectedAmmo.AmountPerPurchase);
        int reserveAfter = playerWeaponController != null ? playerWeaponController.ReserveAmmo : 0;

        if (statusText != null)
            statusText.text = "Berbeli ammo " + selectedAmmo.AmmoName + " +" + selectedAmmo.AmountPerPurchase;

        UpdateCredits(wallet.Credits);
        UpdateAmmoStatus();

        Debug.Log($"[WeaponShopController] Beli ammo {selectedAmmo.AmmoName} +{selectedAmmo.AmountPerPurchase} untuk {equipped.WeaponName}. Reserve {reserveBefore}->{reserveAfter}. Sisa kredit {wallet.Credits}");
    }

    private void UpdateAmmoStatus()
    {
        if (playerWeaponController == null)
        {
            // Coba ambil dari WeaponManager lagi.
            if (WeaponManager.Instance != null)
                playerWeaponController = WeaponManager.Instance.ActiveWeaponController;
        }

        if (playerWeaponController == null)
        {
            Debug.LogWarning($"[WeaponShopController] UpdateAmmoStatus gagal: playerWeaponController null.");
            return;
        }

        // ammoStatusText bersifat opsional (tidak wajib ada di prefab).
        if (ammoStatusText == null)
            return;

        ammoStatusText.text = playerWeaponController.CurrentAmmo + "/" + playerWeaponController.ReserveAmmo;
        Debug.Log($"[WeaponShopController] UpdateAmmoStatus: {playerWeaponController.CurrentAmmo}/{playerWeaponController.ReserveAmmo}");
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
        UpdateAmmoStatus();
    }
}
