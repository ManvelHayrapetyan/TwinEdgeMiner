public class ItemPickedSignal
{
    public ItemPickedSignal(ItemSO itemSO)
    {
        ItemSO = itemSO;
    }
    public ItemSO ItemSO { get; private set; }
}