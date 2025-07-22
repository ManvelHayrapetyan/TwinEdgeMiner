using UnityEngine;
using Zenject;

public class GameDataSaveManager : MonoBehaviour
{
    [Inject] private readonly GameData _gameData;
    [Inject] private readonly ISaveLoadManager _saveLoadManager;

    [SerializeField] private float saveInterval = 60f; 

    private float saveTimer = 0f;

    private bool dataChanged = false;

    void Update()
    {
        saveTimer += Time.deltaTime;

        if (dataChanged || saveTimer >= saveInterval)
        {
            SaveData();
            dataChanged = false;
            saveTimer = 0f;
        }
    }

    void OnApplicationQuit()
    {
        SaveData();
    }

    public void MarkDataChanged()
    {
        dataChanged = true;
    }

    public void SaveData()
    {
        _saveLoadManager.SaveGame(_gameData);
    }
}