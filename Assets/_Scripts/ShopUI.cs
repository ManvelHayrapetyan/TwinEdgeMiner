using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ShopUI : MonoBehaviour, IClosableUI
{
    public bool IsOpen => gameObject.activeSelf;

    [Inject] private readonly GameData _gameData;
    [Inject] private readonly PlayerAndToolStats _playerAndToolStats;

    [SerializeField] private Transform _contentRoot;
    [SerializeField] private GameObject _shopPanelPrefab;
    [SerializeField] private TextMeshProUGUI _totalText;
    [SerializeField] private Button _sellButton;

    public void Open()
    {
        gameObject.SetActive(true);
        _sellButton.onClick.AddListener(() => SellAll());
        UpdateUI();
    }
    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void UpdateUI()
    {
        foreach (Transform child in _contentRoot)
            Destroy(child.gameObject);
        int total = 0;
        var groupedItems = _gameData.Inventory.GetGroupedItems();
        foreach (var (item, count) in groupedItems)
        {
            ShopPanelUI panel = Instantiate(_shopPanelPrefab, _contentRoot).GetComponent<ShopPanelUI>();
            panel.Setup(item, count);
            total += count;
        }
        _totalText.text = total.ToString();
    }

    private void SellAll()
    {
        int income = _gameData.Inventory.SellAllItems();
        _gameData.AddMoney(income);
        UpdateUI();
    }
}