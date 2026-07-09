using TMPro;
using UnityEngine;
using Zenject;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    [Inject] private readonly GameData _gameData;
    [Inject] private readonly InputActions _inputActions;
    [Inject] private readonly InputService _inputService;

    [SerializeField] private TextMeshProUGUI _inventoryText;
    [SerializeField] private TextMeshProUGUI _moneyText;

    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _closeButton2;

    private IClosableUI _currentUI;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> _cancelHandler;

    private void OnEnable()
    {
        _cancelHandler = ctx => TryCloseCurrent();
        _inputActions.Global.Cancel.performed += _cancelHandler;
        _closeButton.onClick.AddListener(TryCloseCurrent);
        _closeButton2.onClick.AddListener(TryCloseCurrent);
    }
    private void Update()
    {
        UpgradeMoneyUI();
        UpgradeInventoryUI();
    }

    private void OnDisable()
    {
        _inputActions.Global.Cancel.performed -= _cancelHandler;
        _closeButton.onClick.RemoveListener(TryCloseCurrent);
        _closeButton2.onClick.RemoveListener(TryCloseCurrent);
    }

    public void ShowUI(IClosableUI ui)
    {
        if (_currentUI != null && _currentUI != ui)
            _currentUI.Close();

        _currentUI = ui;
        _currentUI.Open();
        _inputService.SwitchToUI();
    }

    public void TryCloseCurrent()
    {
        if (_currentUI != null && _currentUI.IsOpen)
        {
            _currentUI.Close();
            _currentUI = null;
            _inputService.SwitchToGameplay();

        }
        else
        {
            // TODO: Open pause menu
        }
    }

    public void UpgradeInventoryUI() =>
        _inventoryText.text = $"{_gameData.Inventory.InventoryItemCount}/{_gameData.Inventory.InventorySize}";

    public void UpgradeMoneyUI() => _moneyText.text = _gameData.Money.ToString();
}