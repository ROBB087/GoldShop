namespace GoldShopWpf.Services;

public static class FinancialDataChangeNotifier
{
    public static event Action? DataChanged;

    public static void NotifyDataChanged()
    {
        DataChanged?.Invoke();
    }
}
