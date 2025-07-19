using UnityEngine;

public class UpgradeLevel
{
    public int PlayerStaminaLevel => _playerStaminaLevel;
    public int PlayerInventoryLevel => _playerInventoryLevel;
    public int MiningToolSpeedLevel => _miningToolSpeedLevel;
    public int MiningToolDestructionDamageLevel => _miningToolDestructionDamageLevel;
    public int MiningToolStabilityDamageLevel => _miningToolStabilityDamageLevel;

    [SerializeField] private int _playerStaminaLevel = new();
    [SerializeField] private int _playerInventoryLevel = new();
    [SerializeField] private int _miningToolSpeedLevel = new();
    [SerializeField] private int _miningToolDestructionDamageLevel = new();
    [SerializeField] private int _miningToolStabilityDamageLevel = new();

}
