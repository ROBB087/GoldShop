using GoldShopCore.Models;

namespace GoldShopWpf.ViewModels;

public class TransactionListItem
{
    public int Id { get; init; }
    public int SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public TransactionType Type { get; init; }
    public decimal OriginalWeight { get; init; }
    public int OriginalKarat { get; init; }
    public decimal Equivalent21 { get; init; }
    public decimal ManufacturingPerGram { get; init; }
    public decimal ImprovementPerGram { get; init; }
    public decimal TotalManufacturing { get; init; }
    public decimal TotalImprovement { get; init; }
    public string Traceability { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
