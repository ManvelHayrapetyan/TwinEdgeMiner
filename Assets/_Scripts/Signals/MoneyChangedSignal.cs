public class MoneyChangedSignal
{
    public MoneyChangedSignal(int changeAmount)
    {
        ChangeAmount = changeAmount;
    }
    public int ChangeAmount { get; private set; }
}