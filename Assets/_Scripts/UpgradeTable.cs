using UnityEngine;
using Zenject;

public class UpgradeTable : MonoBehaviour, IInteractable
{
    [SerializeField] private UIManager _manager;
    [SerializeField] private UpgradesUI _upgradesUI;
    public void Interact()
    {
        _manager.ShowUI(_upgradesUI);
    }
}