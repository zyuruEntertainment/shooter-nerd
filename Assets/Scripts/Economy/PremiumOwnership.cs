using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Helper kepemilikan item premium (luar game). Persisten via SaveSystem.currentData.ownedPremiumItems.
/// </summary>
public static class PremiumOwnership
{
    public static event Action OnOwnershipChanged;
    private static bool loaded;
    private static void EnsureLoaded()
    {
        if (loaded)
            return;
        SaveSystem.Load();
        loaded = true;
    }

    public static bool IsOwned(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;
        EnsureLoaded();
        if (SaveSystem.currentData == null || SaveSystem.currentData.ownedPremiumItems == null)
            return false;
        return SaveSystem.currentData.ownedPremiumItems.Contains(itemId);
    }

    public static void MarkOwned(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;
        EnsureLoaded();
        if (SaveSystem.currentData == null)
            SaveSystem.currentData = new SaveSystem.GameData();
        if (SaveSystem.currentData.ownedPremiumItems == null)
            SaveSystem.currentData.ownedPremiumItems = new System.Collections.Generic.List<string>();
        if (!SaveSystem.currentData.ownedPremiumItems.Contains(itemId))
        {
            SaveSystem.currentData.ownedPremiumItems.Add(itemId);
            SaveSystem.Save();
            OnOwnershipChanged?.Invoke();
        }
    }

    public static void Revoke(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;
        if (SaveSystem.currentData == null || SaveSystem.currentData.ownedPremiumItems == null)
            return;
        if (SaveSystem.currentData.ownedPremiumItems.Remove(itemId))
        {
            SaveSystem.Save();
            OnOwnershipChanged?.Invoke();
        }
    }
}