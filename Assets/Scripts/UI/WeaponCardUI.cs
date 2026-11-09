using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class WeaponCardUI : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponData weaponData;

    [Header("Card UI")]
    [SerializeField] private Image cardPanel;
    [SerializeField] private Button cardButton;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.white;

    public WeaponData Weapon => weaponData;

    private UnityAction onSelected;

    private void Awake()
    {
        if (cardButton == null)
            cardButton = GetComponent<Button>();

        if (cardPanel != null)
            cardPanel.color = normalColor;
    }

    public void Initialize(
        WeaponData data,
        UnityAction selectionCallback)
    {
        weaponData = data;
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
}