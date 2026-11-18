using System;
using UnityEngine;

/// <summary>
/// Pengatur pembelian luar game (premium) yang terhubung dengan weapon/ammo card.
/// Mendukung pembelian item satuan (dengan harga) maupun bundle (gabungan beberapa item).
/// </summary>
public class OutGamePurchaseController : MonoBehaviour
{
    public static OutGamePurchaseController Instance { get; private set; }

    public event Action<string> OnItemPurchased;   // itemId
    public event Action<PremiumBundle> OnBundlePurchased;
    public event Action<string> OnPurchaseFailed;  // reason

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Beli item premium satuan dari weapon card.</summary>
    public bool Purchase(WeaponCardUI card)
    {
        if (card == null || !card.IsPremium)
        {
            OnPurchaseFailed?.Invoke("Kartu bukan item premium.");
            return false;
        }
        return PurchaseItem(card.PremiumId, card.PremiumPrice);
    }

    /// <summary>Beli item premium satuan dari ammo card.</summary>
    public bool Purchase(AmmoCardUI card)
    {
        if (card == null || !card.IsPremium)
        {
            OnPurchaseFailed?.Invoke("Kartu bukan item premium.");
            return false;
        }
        return PurchaseItem(card.PremiumId, card.PremiumPrice);
    }

    /// <summary>Beli item premium satuan berdasarkan ID dan harga cash.</summary>
    public bool PurchaseItem(string itemId, int priceCash)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            OnPurchaseFailed?.Invoke("ID item kosong.");
            return false;
        }

        if (PremiumOwnership.IsOwned(itemId))
        {
            OnPurchaseFailed?.Invoke("Item sudah dimiliki.");
            return false;
        }

        if (PlayerCashWallet.Instance == null)
        {
            OnPurchaseFailed?.Invoke("Dompet cash belum tersedia.");
            return false;
        }

        if (!PlayerCashWallet.Instance.TrySpend(priceCash))
        {
            OnPurchaseFailed?.Invoke("Cash tidak cukup.");
            return false;
        }

        PremiumOwnership.MarkOwned(itemId);
        OnItemPurchased?.Invoke(itemId);
        Debug.Log($"[OutGamePurchase] Item premium '{itemId}' dibeli. Sisa cash {PlayerCashWallet.Instance.Cash}");
        return true;
    }

    /// <summary>Beli bundle (gabungan beberapa item) dengan satu harga.</summary>
    public bool PurchaseBundle(PremiumBundle bundle)
    {
        if (bundle == null)
        {
            OnPurchaseFailed?.Invoke("Bundle null.");
            return false;
        }

        if (PlayerCashWallet.Instance == null)
        {
            OnPurchaseFailed?.Invoke("Dompet cash belum tersedia.");
            return false;
        }

        if (!PlayerCashWallet.Instance.TrySpend(bundle.Price))
        {
            OnPurchaseFailed?.Invoke("Cash tidak cukup untuk bundle.");
            return false;
        }

        foreach (string id in bundle.ItemIds)
        {
            PremiumOwnership.MarkOwned(id);
        }

        OnBundlePurchased?.Invoke(bundle);
        Debug.Log($"[OutGamePurchase] Bundle '{bundle.BundleName}' dibeli. Sisa cash {PlayerCashWallet.Instance.Cash}");
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}