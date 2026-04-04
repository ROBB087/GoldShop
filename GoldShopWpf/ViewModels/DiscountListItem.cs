using GoldShopCore.Models;

namespace GoldShopWpf.ViewModels;

public class DiscountListItem : SelectableViewModel
{
    public int Id { get; init; }
    public int SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DiscountType Type { get; init; }
    public decimal Amount { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
