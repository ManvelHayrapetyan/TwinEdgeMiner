using UnityEngine;

[System.Serializable]
public class UpgradeLevel
{
    public int PlayerStaminaLevel => _playerStaminaLevel;
    public int PlayerInventoryLevel => _playerInventoryLevel;
    public int MiningToolSpeedLevel => _miningToolSpeedLevel;
    public int MiningToolDestructionDamageLevel => _miningToolDestructionDamageLevel;
    public int MiningToolStabilityDamageLevel => _miningToolStabilityDamageLevel;

    [SerializeField] private int _playerStaminaLevel;
    [SerializeField] private int _playerInventoryLevel;
    [SerializeField] private int _miningToolSpeedLevel;
    [SerializeField] private int _miningToolDestructionDamageLevel;
    [SerializeField] private int _miningToolStabilityDamageLevel;
}
