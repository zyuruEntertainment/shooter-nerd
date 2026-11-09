using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Berpindah ke senjata berikutnya yang dimiliki pemain.
/// Bisa dipanggil lewat UnityEvent (OnClick tombol di Inspector) atau shortcut keyboard F.
/// Mengandalkan WeaponManager sebagai sumber kebenaran kepemilikan dan senjata aktif.
/// </summary>
public class WeaponSwitcher : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private WeaponManager weaponManager;

    private void Awake()
    {
        if (weaponManager == null)
            weaponManager = WeaponManager.Instance;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            SwitchWeapon();
    }

    /// <summary>Pindah ke senjata berikutnya dalam daftar milik. Cocok dipasang ke OnClick tombol.</summary>
    public void SwitchWeapon()
    {
        if (weaponManager == null)
            return;

        var owned = weaponManager.OwnedWeapons;
        if (owned.Count == 0)
            return;

        int current = -1;
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i] == weaponManager.EquippedWeapon)
            {
                current = i;
                break;
            }
        }

        int next = (current + 1) % owned.Count;
        weaponManager.Equip(owned[next]);
    }
}
