public interface IClosableUI
{
    void Open();
    void Close();
    bool IsOpen { get; }
}
