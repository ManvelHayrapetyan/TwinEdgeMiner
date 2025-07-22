using System.Collections.Generic;
using UnityEngine;
public class PlayerAndToolStats
{
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
}
