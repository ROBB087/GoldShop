namespace GoldShopWpf.ViewModels;

public class WeeklyReportRow
{
    public int SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public decimal TotalGold { get; init; }
    public decimal TotalPayments { get; init; }
    public decimal CurrentBalance { get; init; }
}
