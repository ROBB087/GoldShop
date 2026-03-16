using GoldShopCore.Models;

namespace GoldShopWpf.ViewModels;

public class TransactionRow
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public TransactionType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal Balance { get; init; }
    public string Notes { get; init; } = string.Empty;
}
