using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private TextMeshProUGUI _totalText;

    public void Setup(ItemSO item, int count)
    {
        _icon.sprite = item.Icon;
        _nameText.text = item.ItemName;
        _amountText.text = count.ToString();
        _priceText.text = item.BasePrice.ToString();
        _totalText.text = (item.BasePrice * count).ToString();
    }
}
