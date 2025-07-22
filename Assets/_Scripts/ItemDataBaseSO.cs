using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataBaseSO", menuName = "Items/ItemDataBaseSO")]
public class ItemDataBaseSO : ScriptableObject
{
    [SerializeField] private List<ItemSO> _itemSOs;

    private Dictionary<string, ItemSO> _itemsByID;
    public ItemSO GetItemById(string id)
    {
        _itemsByID ??= _itemSOs.ToDictionary(i => i.ID, i => i);
        if (!_itemsByID.TryGetValue(id, out ItemSO ItemOut))     
            throw new KeyNotFoundException($"ID '{id}' not found in ItemDataBaseSO dictionary.");
        return ItemOut;
    }
}