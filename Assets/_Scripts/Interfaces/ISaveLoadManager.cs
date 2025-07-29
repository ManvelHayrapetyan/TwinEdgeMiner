public interface ISaveLoadManager
{
    void SaveGame(GameData data);
    GameData LoadGame();
}