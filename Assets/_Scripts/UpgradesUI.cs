using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UpgradesUI : MonoBehaviour, IClosableUI
{
    public bool IsOpen => gameObject.activeSelf;

    [SerializeField] private UpgradePanelUI[] _upgradePanelUI;

    [Inject] private readonly GameData _gameData;
    [Inject] private readonly PlayerAndToolStats _playerAndToolStats;

    private TextMeshProUGUI[] _upgradeLevelText = new TextMeshProUGUI[6];
    private TextMeshProUGUI[] _upgradeInfoText = new TextMeshProUGUI[6];
    private TextMeshProUGUI[] _currentStatText = new TextMeshProUGUI[6];
    private TextMeshProUGUI[] _nextStatText = new TextMeshProUGUI[6];


    private Button[] _upgradeButton = new Button[6];
    private TextMeshProUGUI[] _priceText = new TextMeshProUGUI[6];


    private Func<int>[] getUpgradeLevels;
    private Func<string>[] getUpgradeInfos;
    private Func<float>[] getCurrentStats;
    private Func<float>[] getNextStats;
    private Func<int, int>[] getUpgradeCosts;
    private Action[] increaseLevel;

    private void Awake()
    {
        for (int i = 0; i < 6; i++)
        {
            _upgradeLevelText[i] = _upgradePanelUI[i].UpgradeLevelText;
            _upgradeInfoText[i] = _upgradePanelUI[i].UpgradeInfoText;
            _currentStatText[i] = _upgradePanelUI[i].CurrentStatText;
            _nextStatText[i] = _upgradePanelUI[i].NextStatText;

            _upgradeButton[i] = _upgradePanelUI[i].UpgradeButton;
            _priceText[i] = _upgradePanelUI[i].PriceText;
        }
        SetupDelegates();
        SetUpButtons();
    }
    private void Start()
    {
        UpdateUI();
    }
    public void Open()
    {
        gameObject.SetActive(true);
        UpdateUI();
    }
    public void Close()
    {
        gameObject.SetActive(false);
    }
    public void UpdateUI()
    {
        for (int i = 0; i < getUpgradeLevels.Length; i++)
        {
            int level = getUpgradeLevels[i]();
            _upgradeLevelText[i].text = level.ToString();
            _upgradeInfoText[i].text = getUpgradeInfos[i]();
            _currentStatText[i].text = getCurrentStats[i]().ToString();

            if (level >= _playerAndToolStats.MaxUpgradeLevel)
            {
                _currentStatText[i].GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                _nextStatText[i].gameObject.SetActive(false);

                _priceText[i].text = "Max";
                _upgradeButton[i].interactable = false;
                _upgradeButton[i].image.color = Color.gray;
            }
            else
            {
                _nextStatText[i].gameObject.SetActive(true);
                _nextStatText[i].text = getNextStats[i]().ToString();

                int cost = getUpgradeCosts[i](level);
                if (_gameData.Money >= cost)
                    _upgradeButton[i].image.color = Color.green;
                else
                    _upgradeButton[i].image.color = Color.red;

                _priceText[i].text = cost.ToString();
            }
        }
    }
    private void SetupDelegates()
    {
        getUpgradeLevels = new Func<int>[]
        {
        () => _gameData.UpgradeLevel.PlayerInventoryLevel,
        () => _gameData.UpgradeLevel.PlayerStaminaLevel,
        () => _gameData.UpgradeLevel.MiningToolSpeedLevel,
        () => _gameData.UpgradeLevel.MiningToolStabilityDamageLevel,
        () => _gameData.UpgradeLevel.MiningToolDestructionDamageLevel,
        () => _gameData.UpgradeLevel.MiningToolRadiusLevel,
        };

        getUpgradeInfos = new Func<string>[]
        {
            () => "Player Inventory Size",
            () => "Player Stamina",
            () => "Mining Tool Speed",
            () => "Mining Tool Stability Damage",
            () => "Mining Tool Destruction Damage",
            () => "Mining Tool Radius",
        };

        getCurrentStats = new Func<float>[]
        {
        () => _playerAndToolStats.PlayerInventorySize,
        () => _playerAndToolStats.PlayerStamina,
        () => _playerAndToolStats.MiningToolSpeed,
        () => _playerAndToolStats.MiningToolStabilityDamage,
        () => _playerAndToolStats.MiningToolDestructionDamage,
        () => _playerAndToolStats.MiningToolRadius,
        };

        getNextStats = new Func<float>[]
        {
        () => _playerAndToolStats.PlayerInventorySizeNextLevel,
        () => _playerAndToolStats.PlayerStaminaNextLevel,
        () => _playerAndToolStats.MiningToolSpeedNextLevel,
        () => _playerAndToolStats.MiningToolStabilityDamageNextLevel,
        () => _playerAndToolStats.MiningToolDestructionDamageNextLevel,
        () => _playerAndToolStats.MiningToolRadiusNextLevel,
        };

        getUpgradeCosts = new Func<int, int>[]
        {
        level => _playerAndToolStats.UpgradeCost(level),
        level => _playerAndToolStats.UpgradeCost(level),
        level => _playerAndToolStats.UpgradeCost(level),
        level => _playerAndToolStats.UpgradeCost(level),
        level => _playerAndToolStats.UpgradeCost(level),
        level => _playerAndToolStats.UpgradeCost(level),
        };
        increaseLevel = new Action[]
        {
          ()  => {
              _gameData.UpgradeLevel.PlayerInventoryLevelIncrease();
              _gameData.Inventory.UpgradeInventorySize(_playerAndToolStats.PlayerInventorySize);
                },
          ()  => _gameData.UpgradeLevel.PlayerStaminaLevelIncrease(),
          ()  => _gameData.UpgradeLevel.MiningToolSpeedLevelIncrease(),
          ()  => _gameData.UpgradeLevel.MiningToolStabilityDamageLevelIncrease(),
          ()  => _gameData.UpgradeLevel.MiningToolDestructionDamageLevelIncrease(),
          ()  => _gameData.UpgradeLevel.MiningToolRadiusLevelIncrease(),
        };
    }

    private void SetUpButtons()
    {
        for (int i = 0; i < getUpgradeLevels.Length; i++)
        {
            int index = i;
            _upgradeButton[i].onClick.RemoveAllListeners();
            _upgradeButton[i].onClick.AddListener(() =>
            {
                int level = getUpgradeLevels[index]();
                if (level < _playerAndToolStats.MaxUpgradeLevel &&
                    _gameData.Money >= getUpgradeCosts[index](level))
                {
                    increaseLevel[index]();
                    _gameData.SpendMoney(getUpgradeCosts[index](level));
                    UpdateUI();
                }
            });
        }
    }
}
