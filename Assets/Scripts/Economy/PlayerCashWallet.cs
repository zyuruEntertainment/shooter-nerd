using System;
using UnityEngine;

/// <summary>
/// Dompet cash (mata uang premium) pemain untuk pembelian di luar game.
/// Terpisah dari PlayerWallet (credits in-game). Persisten via SaveSystem.
/// </summary>
public class PlayerCashWallet : MonoBehaviour
{
    public static PlayerCashWallet Instance { get; private set; }

    [Header("Economy")]
    [SerializeField, Min(0)] private int startingCash = 0;

    public int Cash { get; private set; }
    public event Action<int> OnCashChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SaveSystem.Load();
        // Muat dari save jika ada, jika tidak pakai startingCash.
        if (SaveSystem.currentData != null)
            Cash = Mathf.Max(0, SaveSystem.currentData.cash);
        else
            Cash = Mathf.Max(0, startingCash);

        if (Cash == 0 && startingCash > 0)
            Cash = startingCash;

        SaveSystem.currentData.cash = Cash;
        SaveSystem.Save();

        OnCashChanged?.Invoke(Cash);
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && Cash >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || !CanAfford(amount))
            return false;

        Cash -= amount;
        OnCashChanged?.Invoke(Cash);
        SaveSystem.currentData.cash = Cash;
        SaveSystem.Save();
        return true;
    }

    public void AddCash(int amount)
    {
        if (amount <= 0)
            return;

        Cash += amount;
        OnCashChanged?.Invoke(Cash);
        SaveSystem.currentData.cash = Cash;
        SaveSystem.Save();
    }

    /// <summary>Reset cash ke nilai awal (untuk round baru / debug).</summary>
    public void ResetCash()
    {
        Cash = Mathf.Max(0, startingCash);
        OnCashChanged?.Invoke(Cash);
        SaveSystem.currentData.cash = Cash;
        SaveSystem.Save();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}