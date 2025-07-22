using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeSO", menuName = "UpgradeSO")]
public class UpgradeSO : ScriptableObject
{
    public int MaxUpgradeLevel => _maxUpgradeLevel;
    public IReadOnlyList<int> PlayerStaminaBonusPerLevel => _playerStaminaBonusPerLevel;
    public IReadOnlyList<int> PlayerInventoryBonusPerLevel => _playerInventoryBonusPerLevel;
    public IReadOnlyList<float> MiningToolSpeedBonusPerLevel => _miningToolSpeedBonusPerLevel;
    public IReadOnlyList<float> MiningToolDestructionDamageBonusPerLevel => _miningToolDestructionDamageBonusPerLevel;
    public IReadOnlyList<float> MiningToolStabilityDamageBonusPerLevel => _miningToolStabilityDamageBonusPerLevel;
    public IReadOnlyList<int> UpgradeCostPerLevel => _upgradeCostPerLevel;

    [SerializeField, Min(1)] private int _maxUpgradeLevel = 10;
    [SerializeField] private List<int> _playerStaminaBonusPerLevel = new List<int>();
    [SerializeField] private List<int> _playerInventoryBonusPerLevel = new List<int>();
    [SerializeField] private List<float> _miningToolSpeedBonusPerLevel = new List<float>();
    [SerializeField] private List<float> _miningToolDestructionDamageBonusPerLevel = new List<float>();
    [SerializeField] private List<float> _miningToolStabilityDamageBonusPerLevel = new List<float>();
    [SerializeField] private List<int> _upgradeCostPerLevel = new List<int>();

    private void OnValidate()
    {
        ValidateListSize(ref _playerStaminaBonusPerLevel, nameof(_playerStaminaBonusPerLevel));
        ValidateListSize(ref _playerInventoryBonusPerLevel, nameof(_playerInventoryBonusPerLevel));
        ValidateListSize(ref _miningToolSpeedBonusPerLevel, nameof(_miningToolSpeedBonusPerLevel));
        ValidateListSize(ref _miningToolDestructionDamageBonusPerLevel, nameof(_miningToolDestructionDamageBonusPerLevel));
        ValidateListSize(ref _miningToolStabilityDamageBonusPerLevel, nameof(_miningToolStabilityDamageBonusPerLevel));
        ValidateListSize(ref _upgradeCostPerLevel, nameof(_upgradeCostPerLevel));
    }

    private void ValidateListSize<T>(ref List<T> list, string listName)
    {
        if (list == null)
        {
            Debug.LogWarning($"{listName} was null, creating new list.");
            list = new List<T>();
        }

        if (list.Count < _maxUpgradeLevel)
        {
            Debug.LogWarning($"{listName} has fewer elements ({list.Count}) than MaxUpgradeLevel ({_maxUpgradeLevel}). Adding defaults.");
            while (list.Count < _maxUpgradeLevel)
                list.Add(default);
        }
        else if (list.Count > _maxUpgradeLevel)
        {
            Debug.LogWarning($"{listName} has more elements ({list.Count}) than MaxUpgradeLevel ({_maxUpgradeLevel}). Removing extras.");
            while (list.Count > _maxUpgradeLevel)
                list.RemoveAt(list.Count - 1);
        }
    }
}
