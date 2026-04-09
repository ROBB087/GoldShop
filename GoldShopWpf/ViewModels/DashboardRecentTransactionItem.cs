using GoldShopCore.Models;

namespace GoldShopWpf.ViewModels;

public class DashboardRecentTransactionItem
{
    public string TraderName { get; init; } = string.Empty;
    public string Category { get; init; } = TransactionCategories.GoldOutbound;
    public TransactionType Type { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string DetailsLine { get; init; } = string.Empty;
    public string MetricDisplay { get; init; } = string.Empty;
    public string DateDisplay { get; init; } = string.Empty;
}
