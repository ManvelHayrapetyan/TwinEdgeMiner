using UnityEngine;
using Zenject;

public class GameInstaller : MonoInstaller
{
    [SerializeField] private ItemDataBaseSO _itemDataBaseSO;
    [SerializeField] private LookAtTargetDetector _lookAtTargetDetector;
    [SerializeField] private PlayerAndToolBaseStatsSO _playerAndToolBaseStatsSO;
    [SerializeField] private UpgradeSO _upgradeSO;

    public override void InstallBindings()
    {
        SaveLoadManager saveLoadManager = new("/saveGameData.json");
        Container.Bind<ISaveLoadManager>().FromInstance(saveLoadManager).AsSingle();
        GameData gameData = saveLoadManager.LoadGame();
        gameData.Inventory.Init(_itemDataBaseSO);
        gameData.Inventory.LoadInventoryFromID();

        PlayerAndToolStats playerAndToolStats = new(_playerAndToolBaseStatsSO, _upgradeSO, gameData.UpgradeLevel);
        Container.Bind<PlayerAndToolStats>().FromInstance(playerAndToolStats).AsSingle();
        Container.Bind<UpgradeLevel>().FromInstance(gameData.UpgradeLevel).AsSingle();
        Container.Bind<GameData>().FromInstance(gameData).AsSingle();




        Container.Bind<LookAtTargetDetector>().FromInstance(_lookAtTargetDetector).AsSingle();

        InputActions inputActions = new();
        inputActions.Enable();
        Container.Bind<InputActions>().FromInstance(inputActions).AsSingle();
    }
}