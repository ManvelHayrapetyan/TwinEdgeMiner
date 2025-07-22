using System.IO;
using UnityEngine;
using UnityEngine.Playables;

public class SaveLoadManager : ISaveLoadManager
{
    private readonly string _savePath;

    public SaveLoadManager(string path)
    {
        _savePath = Application.persistentDataPath + path;
    }

    public void SaveGame(GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_savePath, json);
        Debug.Log($"Game saved at {_savePath}");
    }

    public GameData LoadGame()
    {
        if (!File.Exists(_savePath))
        {
            GameData EmptyData = new();
            SaveGame(EmptyData);
            Debug.LogWarning("No save file found, creating new GameData.");
        }

        string json = File.ReadAllText(_savePath);
        GameData data = JsonUtility.FromJson<GameData>(json);
        Debug.Log("Game loaded.");
        return data;
    }
}
