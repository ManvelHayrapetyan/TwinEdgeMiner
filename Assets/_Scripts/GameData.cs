using UnityEngine;

public class GameData
{
    public UpgradeLevel UpgradeLevel => _upgradeLevel;
    public int Money => _money;
    public Inventory Inventory => _inventory;

    [SerializeField] private UpgradeLevel _upgradeLevel;
    [SerializeField] private int _money;
    [SerializeField] private Inventory _inventory = new(1);
}