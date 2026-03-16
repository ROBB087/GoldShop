using GoldShopCore.Models;

namespace GoldShopWpf.ViewModels;

public class TransactionListItem
{
    public int Id { get; init; }
    public int SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public TransactionType Type { get; init; }
    public string Details { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Notes { get; init; } = string.Empty;
}
