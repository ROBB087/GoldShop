namespace GoldShopWpf.ViewModels;

public class WeeklyReportRow
{
    public int SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public decimal TotalGold21 { get; init; }
    public decimal TotalManufacturing { get; init; }
    public decimal TotalImprovement { get; init; }
    public decimal FinalManufacturing { get; init; }
    public decimal FinalImprovement { get; init; }
}
