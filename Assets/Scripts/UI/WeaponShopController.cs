using TMPro;
using UnityEngine;

public class WeaponShopController : MonoBehaviour
{
    [Header("Shop Inventory")]
    [SerializeField] private WeaponData[] weapons;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private WeaponShopItemUI itemPrefab;

    [Header("UI")]
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private TMP_Text statusText;

    [Header("References")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private PlayerWallet wallet;

    private void Start()
    {
        if (weaponManager == null)
            weaponManager = WeaponManager.Instance;

        if (wallet == null)
            wallet = PlayerWallet.Instance;

        if (wallet != null)
            wallet.OnCreditsChanged += UpdateCredits;

        Refresh();
        UpdateCredits(wallet != null ? wallet.Credits : 0);
    }

    public void Refresh()
    {
        if (contentRoot == null || itemPrefab == null || weaponManager == null)
        {
            Debug.LogError("WeaponShopController: contentRoot, itemPrefab, atau WeaponManager belum diatur.", this);
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        foreach (WeaponData weapon in weapons)
        {
            if (weapon == null)
                continue;

            WeaponShopItemUI item = Instantiate(itemPrefab, contentRoot);
            bool owned = weaponManager.IsOwned(weapon);
            bool equipped = weaponManager.IsEquipped(weapon);

            item.Setup(weapon, owned, equipped, () => PurchaseOrEquip(weapon));
        }
    }

    private void PurchaseOrEquip(WeaponData weapon)
    {
        if (weaponManager == null)
            return;

        bool success = weaponManager.TryPurchaseOrEquip(weapon, out string message);

        if (statusText != null)
            statusText.text = message;

        if (success)
            Refresh();

        UpdateCredits(wallet != null ? wallet.Credits : 0);
    }

    private void UpdateCredits(int amount)
    {
        if (creditsText != null)
            creditsText.text = amount.ToString("N0");
    }

    private void OnDestroy()
    {
        if (wallet != null)
            wallet.OnCreditsChanged -= UpdateCredits;
    }
}
