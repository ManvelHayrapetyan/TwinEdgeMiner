public class SignalItemPicked
{
    public SignalItemPicked(ItemSO itemSO)
    {
        ItemSO = itemSO;
    }
    public ItemSO ItemSO { get; private set; }
}