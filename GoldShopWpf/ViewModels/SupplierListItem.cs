namespace GoldShopWpf.ViewModels;

public class SupplierListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string WorkerName { get; init; } = string.Empty;
    public string WorkerPhone { get; init; } = string.Empty;
    public decimal TotalGold21 { get; init; }
    public decimal NetGold21 { get; init; }
    public DateTime? LastTransactionDate { get; init; }
    public string LastActivityLabel { get; init; } = string.Empty;
}
