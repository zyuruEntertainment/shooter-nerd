using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bundle pembelian luar game: gabungan beberapa item (weapon/ammo premium) dengan satu harga.
/// </summary>
[CreateAssetMenu(fileName = "PremiumBundle", menuName = "Shooter Nerd/Premium Bundle", order = 3)]
public class PremiumBundle : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string bundleName = "New Bundle";
    [SerializeField] private Sprite icon;

    [Header("Harga (cash)")]
    [SerializeField, Min(0)] private int price = 0;

    [Header("Item yang termasuk (ID premium)")]
    [Tooltip("Daftar ID premium item yang akan dimiliki setelah bundle dibeli.")]
    [SerializeField] private List<string> itemIds = new List<string>();

    public string BundleName => bundleName;
    public Sprite Icon => icon;
    public int Price => price;
    public IReadOnlyList<string> ItemIds => itemIds;
}