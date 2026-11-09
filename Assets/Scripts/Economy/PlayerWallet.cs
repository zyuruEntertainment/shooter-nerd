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
        Credits = startingCredits;
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
        return true;
    }

    public void AddCredits(int amount)
    {
        if (amount <= 0)
            return;

        Credits += amount;
        OnCreditsChanged?.Invoke(Credits);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
