using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static UnityEditor.Progress;

[System.Serializable]
public class Inventory
{
    public int InventorySize => _inventorySize;
    public int InventoryItemCount => _inventoryIDs != null ? _inventoryIDs.Count : 0;

    [NonSerialized] private List<ItemSO> _inventory;
    [SerializeField] private List<string> _inventoryIDs;
    [SerializeField] private int _inventorySize;

    private ItemDataBaseSO _itemDataBaseSO;
    public Inventory(int inventorySize)
    {
        _inventorySize = inventorySize;
        _inventory = new List<ItemSO>();
        _inventoryIDs = new List<string>();
    }

    public void Init(ItemDataBaseSO itemDataBaseSO)
    {
        _itemDataBaseSO = itemDataBaseSO;
    }
    public bool TryAddItem(ItemSO item)
    {
        if (_inventory.Count < _inventorySize)
        {
            _inventory.Add(item);
            _inventoryIDs.Add(item.ID);
            return true;
        }
        return false;
    }

    public void Clear()
    {
        _inventory?.Clear();
        _inventoryIDs?.Clear();
    }

    public int SellAllOre()
    {
        int sellPrice = 0;
        foreach (ItemSO ore in _inventory)
        {
            sellPrice += ore.BasePrice;
        }
        _inventory?.Clear();
        _inventoryIDs?.Clear();
        return sellPrice;
    }

    public ItemSO GetItem(int index)
    {
        return _inventory[index];
    }
    public string GetItemID(int index)
    {
        return _inventoryIDs[index];
    }
    public void LoadInventoryFromID()
    {
        _inventory.Clear();
        foreach (var id in _inventoryIDs)
        {
            _inventory.Add(_itemDataBaseSO.GetItemById(id));
        }
    }
}