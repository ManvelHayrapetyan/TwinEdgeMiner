using System;
using UnityEngine;
using Zenject;

public class GameData
{
    public UpgradeLevel UpgradeLevel => _upgradeLevel;
    public int Money => _money;
    public Inventory Inventory => _inventory;

    [SerializeField] private UpgradeLevel _upgradeLevel;
    [SerializeField] private int _money;
    [SerializeField] private Inventory _inventory = new(1);

    public void AddMoney(int amount)
    {
        _money += amount;
    }

    public void SpendMoney(int amount)
    {
        if (_money < amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Not enough money");
        _money -= amount;
    }
}