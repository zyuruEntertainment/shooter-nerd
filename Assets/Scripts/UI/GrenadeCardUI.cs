using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Kartu item toko khusus GrenadeData.
/// </summary>
public class GrenadeCardUI : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private GrenadeData grenadeData;

    [Header("Card UI")]
    [SerializeField] private Image cardPanel;
    [SerializeField] private Button cardButton;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.white;

    public GrenadeData Grenade => grenadeData;

    private UnityAction onSelected;

    private void Awake()
    {
        if (cardButton == null)
            cardButton = GetComponent<Button>();
        if (cardPanel != null)
            cardPanel.color = normalColor;
    }

    public void Initialize(GrenadeData data, UnityAction selectionCallback)
    {
        grenadeData = data;
        onSelected = selectionCallback;
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(Select);
        }
    }

    public void Select() => onSelected?.Invoke();

    public void SetSelected(bool selected)
    {
        if (cardPanel == null) return;
        cardPanel.color = selected ? selectedColor : normalColor;
    }

    public void SetColors(Color normal, Color selected)
    {
        normalColor = normal;
        selectedColor = selected;
        if (cardPanel != null)
            cardPanel.color = normalColor;
    }
}