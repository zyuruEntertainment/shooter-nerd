using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [Header("Economy")]
    [SerializeField, Min(0)] private int startingCredits = 2500;

    public int Credits { get; private set; }
    public event Action<int> OnCreditsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Jika nilai di Inspector tertulis 0 (atau kosong), paksa default 2500
        // agar saldo tidak pernah tampil 0 saat memulai play.
        int saldoAwal = startingCredits > 0 ? startingCredits : 2500;

        // Reset saldo setiap kali Play dimulai. Tidak muat dari berkas penyimpanan
        // lama sehingga saldo tidak tertahan pada 0 dari sesi sebelumnya.
        Credits = saldoAwal;
        SaveSystem.currentData.credits = Credits;
        SaveSystem.Save();

        OnCreditsChanged?.Invoke(Credits);
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && Credits >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || !CanAfford(amount))
            return false;

        Credits -= amount;
        OnCreditsChanged?.Invoke(Credits);
        SaveSystem.currentData.credits = Credits;
        SaveSystem.Save();
        return true;
    }

    public void AddCredits(int amount)
    {
        if (amount <= 0)
            return;

        Credits += amount;
        OnCreditsChanged?.Invoke(Credits);
        SaveSystem.currentData.credits = Credits;
        SaveSystem.Save();
    }

    /// <summary>Reset saldo ke nilai awal (untuk round baru).</summary>
    public void ResetCredits()
    {
        int saldoAwal = startingCredits > 0 ? startingCredits : 2500;
        Credits = saldoAwal;
        OnCreditsChanged?.Invoke(Credits);
        SaveSystem.currentData.credits = Credits;
        SaveSystem.Save();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
