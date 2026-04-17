namespace GoldShopCore.Models;

public enum OpeningBalanceAdjustmentType
{
    Manufacturing = 1,
    Improvement = 2
}

public class OpeningBalanceAdjustment
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public OpeningBalanceAdjustmentType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
