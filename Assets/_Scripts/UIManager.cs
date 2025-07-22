using TMPro;
using UnityEngine;
using Zenject;

public class UIManager : MonoBehaviour
{
    [Inject] private SignalBus _signalBus;
    [Inject] private readonly GameData _gameData;

    [SerializeField] private TMP_Text _inventoryText;

    private void OnEnable()
    {
        _signalBus.Subscribe<SignalItemPicked>(OnItemPicked);
    }

    private void Start()
    {
        UpgradeInventoryUI();
    }

    private void OnDisable()
    {
        _signalBus.Unsubscribe<SignalItemPicked>(OnItemPicked);
    }
    public void UpgradeInventoryUI()
    {
        _inventoryText.text = $"{_gameData.Inventory.InventoryItemCount}/{_gameData.Inventory.InventorySize}";
    }

    private void OnItemPicked(SignalItemPicked _)
    {
        UpgradeInventoryUI();
    }

}