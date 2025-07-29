using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanelUI : MonoBehaviour
{
    public TextMeshProUGUI UpgradeLevelText => _upgradeLevelText;
    public TextMeshProUGUI UpgradeInfoText => _upgradeInfoText;
    public TextMeshProUGUI CurrentStatText => _currentStatText;
    public TextMeshProUGUI NextStatText => _nextStatText;
    public Button UpgradeButton => _upgradeButton;
    public TextMeshProUGUI PriceText => _priceText;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _upgradeLevelText;
    [SerializeField] private TextMeshProUGUI _upgradeInfoText;
    [SerializeField] private TextMeshProUGUI _currentStatText;
    [SerializeField] private TextMeshProUGUI _nextStatText;
    [Header("Button")]
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TextMeshProUGUI _priceText;
}