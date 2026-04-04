namespace GoldShopCore.Models;

public enum DiscountType
{
    Manufacturing = 1,
    Improvement = 2
}

public class DiscountRecord
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public DiscountType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class TraderSummary
{
    public decimal TotalGold21 { get; set; }
    public decimal TotalManufacturing { get; set; }
    public decimal TotalImprovement { get; set; }
    public decimal ManufacturingDiscounts { get; set; }
    public decimal ImprovementDiscounts { get; set; }
    public decimal FinalManufacturing => TotalManufacturing - ManufacturingDiscounts;
    public decimal FinalImprovement => TotalImprovement - ImprovementDiscounts;
}
