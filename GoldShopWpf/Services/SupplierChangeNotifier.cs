namespace GoldShopWpf.Services;

public static class SupplierChangeNotifier
{
    public static event Action? SuppliersChanged;

    public static void NotifySuppliersChanged()
    {
        SuppliersChanged?.Invoke();
    }
}
