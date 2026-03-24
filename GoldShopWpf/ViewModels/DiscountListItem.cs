using GoldShopCore.Models;

namespace GoldShopWpf.ViewModels;

public class DiscountListItem
{
    public int Id { get; init; }
    public DiscountType Type { get; init; }
    public decimal Amount { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
