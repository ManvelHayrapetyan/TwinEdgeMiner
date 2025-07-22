using UnityEngine;

[CreateAssetMenu(fileName = "OreSO", menuName = "Items/Ore")]
public class ItemSO : ScriptableObject
{

    public Sprite Icon => _icon;
    public string ID => _id;
    public string ItemName => _itemName;
    public int BasePrice => _basePrice;

    [SerializeField] private Sprite _icon;
    [SerializeField] private string _id;
    [SerializeField] private string _itemName;
    [SerializeField] private int _basePrice;

}
