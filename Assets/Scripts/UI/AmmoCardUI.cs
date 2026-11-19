using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Kartu item toko khusus AmmoData.
/// </summary>
public class AmmoCardUI : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private AmmoData ammoData;

    [Header("Card UI")]
    [SerializeField] private Image cardPanel;
    [SerializeField] private Button cardButton;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.white;

    public AmmoData Ammo => ammoData;

    private UnityAction onSelected;

    private void Awake()
    {
        if (cardButton == null)
            cardButton = GetComponent<Button>();

        if (cardPanel != null)
            cardPanel.color = normalColor;
    }

    public void Initialize(
        AmmoData data,
        UnityAction selectionCallback)
    {
        ammoData = data;
        onSelected = selectionCallback;

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(Select);
        }
    }

    public void Select()
    {
        onSelected?.Invoke();
    }

    public void SetSelected(bool selected)
    {
        if (cardPanel == null)
            return;

        cardPanel.color = selected
            ? selectedColor
            : normalColor;
    }

    /// <summary>Atur warna kartu (normal/selected) dari luar (mis. ItemCard).</summary>
    public void SetColors(Color normal, Color selected)
    {
        normalColor = normal;
        selectedColor = selected;
        if (cardPanel != null)
            cardPanel.color = normalColor;
    }
}