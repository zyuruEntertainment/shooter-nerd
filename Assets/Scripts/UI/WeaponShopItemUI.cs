using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private Image weaponIcon;

    private Button cardButton;

    private void Awake()
    {
        cardButton = GetComponent<Button>();

        // cardButton opsional: WeaponShopItemUI juga dipakai sebagai panel informasi
        // yang tombol belinya diatur oleh WeaponShopController terpisah, jadi tidak
        // mencatat error jika tombol tidak ada di objek ini.
    }

    public void Setup(
        WeaponData weapon,
        bool isOwned,
        bool isEquipped,
        Action onClick)
    {
        if (weapon == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (weaponNameText != null)
            weaponNameText.text = weapon.WeaponName;

        if (priceText != null)
            priceText.text = isOwned
                ? "OWNED"
                : weapon.Price.ToString("N0") + " credits";

        if (statsText != null)
        {
            statsText.text =
                $"damage: {weapon.Damage:0.#}\n" +
                $"fire rate: {1f / weapon.FireInterval:0.0}/s\n" +
                $"magazine: {weapon.MagazineSize}\n" +
                $"range: {weapon.Range:0.#}";
        }

        if (weaponIcon != null)
        {
            weaponIcon.sprite = weapon.Icon;
            weaponIcon.enabled = weapon.Icon != null;
        }

        if (actionText != null)
        {
            actionText.text = isEquipped
                ? "EQUIPPED"
                : isOwned ? "EQUIP" : "BUY";
        }

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.interactable = !isEquipped;
            cardButton.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}