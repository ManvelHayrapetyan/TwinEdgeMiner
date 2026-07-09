using System;
using System.Collections.Generic;
public class PlayerAndToolStats
{
    public int PlayerStamina =>
    ComputeStat(_playerAndToolBaseStatsSO.PlayerStaminaBase,
            _upgradeSO.PlayerStaminaBonusPerLevel,
            _upgradeLevel.PlayerStaminaLevel);
    public int PlayerInventorySize =>
        ComputeStat(_playerAndToolBaseStatsSO.PlayerInventorySizeBase,
                    _upgradeSO.PlayerInventoryBonusPerLevel,
                    _upgradeLevel.PlayerInventoryLevel);
    public float MiningToolSpeed =>
        ComputeStat(_playerAndToolBaseStatsSO.MiningToolSpeedBase,
                    _upgradeSO.MiningToolSpeedBonusPerLevel,
                    _upgradeLevel.MiningToolSpeedLevel);
    public float MiningToolDestructionDamage =>
        ComputeStat(_playerAndToolBaseStatsSO.MiningToolDestructionDamageBase,
                    _upgradeSO.MiningToolDestructionDamageBonusPerLevel,
                    _upgradeLevel.MiningToolDestructionDamageLevel);
    public float MiningToolStabilityDamage =>
        ComputeStat(_playerAndToolBaseStatsSO.MiningToolStabilityDamageBase,
                    _upgradeSO.MiningToolStabilityDamageBonusPerLevel,
                    _upgradeLevel.MiningToolStabilityDamageLevel);
    public float MiningToolRadius =>
        ComputeStat(_playerAndToolBaseStatsSO.MiningToolRadiusBase,
                    _upgradeSO.MiningToolRadiusBonusPerLevel,
                    _upgradeLevel.MiningToolRadiusLevel);

    public int PlayerStaminaNextLevel =>
        ComputeStatNextLevel(_playerAndToolBaseStatsSO.PlayerStaminaBase,
            _upgradeSO.PlayerStaminaBonusPerLevel,
            _upgradeLevel.PlayerStaminaLevel);
    public int PlayerInventorySizeNextLevel =>
        ComputeStatNextLevel(_playerAndToolBaseStatsSO.PlayerInventorySizeBase,
                    _upgradeSO.PlayerInventoryBonusPerLevel,
                    _upgradeLevel.PlayerInventoryLevel);
    public float MiningToolSpeedNextLevel =>
        ComputeStatNextLevel(_playerAndToolBaseStatsSO.MiningToolSpeedBase,
                    _upgradeSO.MiningToolSpeedBonusPerLevel,
                    _upgradeLevel.MiningToolSpeedLevel);
    public float MiningToolDestructionDamageNextLevel =>
        ComputeStatNextLevel(_playerAndToolBaseStatsSO.MiningToolDestructionDamageBase,
                    _upgradeSO.MiningToolDestructionDamageBonusPerLevel,
                    _upgradeLevel.MiningToolDestructionDamageLevel);
    public float MiningToolStabilityDamageNextLevel =>
        ComputeStatNextLevel(_playerAndToolBaseStatsSO.MiningToolStabilityDamageBase,
                    _upgradeSO.MiningToolStabilityDamageBonusPerLevel,
                    _upgradeLevel.MiningToolStabilityDamageLevel);
    public float MiningToolRadiusNextLevel =>
        ComputeStatNextLevel(_playerAndToolBaseStatsSO.MiningToolRadiusBase,
                    _upgradeSO.MiningToolRadiusBonusPerLevel,
                    _upgradeLevel.MiningToolRadiusLevel);

    public int MaxUpgradeLevel => _upgradeSO.MaxUpgradeLevel;

    private readonly PlayerAndToolBaseStatsSO _playerAndToolBaseStatsSO;
    private readonly UpgradeSO _upgradeSO;
    private readonly UpgradeLevel _upgradeLevel;

    public PlayerAndToolStats(PlayerAndToolBaseStatsSO playerAndToolBaseStatsSO,
        UpgradeSO upgradeSO,
        UpgradeLevel upgradeLevel)
    {
        _playerAndToolBaseStatsSO = playerAndToolBaseStatsSO;
        _upgradeSO = upgradeSO;
        _upgradeLevel = upgradeLevel;
    }

    public int UpgradeCost(int currentLevel)
    {
        return _upgradeSO.UpgradeCostPerLevel[currentLevel];
    }

    private int ComputeStat(int baseValue, IReadOnlyList<int> bonusesPerLevel, int currentLevel)
    {
        int result = baseValue;
        for (int i = 0; i < currentLevel; i++)
            result += bonusesPerLevel[i];
        return result;
    }

    private float ComputeStat(float baseValue, IReadOnlyList<float> bonusesPerLevel, int currentLevel)
    {
        float result = baseValue;
        for (int i = 0; i < currentLevel; i++)
            result += bonusesPerLevel[i];
        return result;
    }

    private int ComputeStatNextLevel(int baseValue, IReadOnlyList<int> bonusesPerLevel, int currentLevel)
    {
        currentLevel++;
        if (currentLevel > _upgradeSO.MaxUpgradeLevel)
            throw new InvalidOperationException(
                $"Cannot compute next level stat: requested level {currentLevel} " +
                $"exceeds MaxUpgradeLevel {_upgradeSO.MaxUpgradeLevel}.");
        int result = baseValue;
        for (int i = 0; i < currentLevel; i++)
            result += bonusesPerLevel[i];
        return result;
    }

    private float ComputeStatNextLevel(float baseValue, IReadOnlyList<float> bonusesPerLevel, int currentLevel)
    {
        currentLevel++;
        if (currentLevel > _upgradeSO.MaxUpgradeLevel)
            throw new InvalidOperationException(
                $"Cannot compute next level stat: requested level {currentLevel} " +
                $"exceeds MaxUpgradeLevel {_upgradeSO.MaxUpgradeLevel}.");
        float result = baseValue;
        for (int i = 0; i < currentLevel; i++)
            result += bonusesPerLevel[i];
        return result;
    }
}
