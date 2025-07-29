using UnityEngine;

[System.Serializable]
public class UpgradeLevel
{
    public int PlayerStaminaLevel => _playerStaminaLevel;
    public int PlayerInventoryLevel => _playerInventoryLevel;
    public int MiningToolSpeedLevel => _miningToolSpeedLevel;
    public int MiningToolDestructionDamageLevel => _miningToolDestructionDamageLevel;
    public int MiningToolStabilityDamageLevel => _miningToolStabilityDamageLevel;
    public int MiningToolRadiusLevel => _miningToolRadiusLevel;

    [SerializeField] private int _playerStaminaLevel;
    [SerializeField] private int _playerInventoryLevel;
    [SerializeField] private int _miningToolSpeedLevel;
    [SerializeField] private int _miningToolDestructionDamageLevel;
    [SerializeField] private int _miningToolStabilityDamageLevel;
    [SerializeField] private int _miningToolRadiusLevel;

    public void PlayerStaminaLevelIncrease()
    {
        _playerStaminaLevel++;
    }

    public void PlayerInventoryLevelIncrease()
    {
        _playerInventoryLevel++;
    }

    public void MiningToolSpeedLevelIncrease()
    {
        _miningToolSpeedLevel++;
    }

    public void MiningToolDestructionDamageLevelIncrease()
    {
        _miningToolDestructionDamageLevel++;
    }

    public void MiningToolStabilityDamageLevelIncrease()
    {
        _miningToolStabilityDamageLevel++;
    }

    public void MiningToolRadiusLevelIncrease()
    {
        _miningToolRadiusLevel++;
    }
}
