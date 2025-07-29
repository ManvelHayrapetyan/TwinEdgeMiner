using UnityEngine;
using Zenject;

public class ShopTable : MonoBehaviour, IInteractable
{
    [SerializeField] private UIManager _manager;
    [SerializeField] private ShopUI _shopUI;
    public void Interact()
    {
        _manager.ShowUI(_shopUI);
    }
}