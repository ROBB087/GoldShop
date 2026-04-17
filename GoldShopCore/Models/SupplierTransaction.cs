namespace GoldShopCore.Models;

public enum TransactionType
{
    Out = 1,
    In = 2
}

public class SupplierTransaction
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public string Category { get; set; } = TransactionCategories.GoldOutbound;
    public string? ItemName { get; set; }
    public string? Description { get; set; }
    public decimal OriginalWeight { get; set; }
    public int OriginalKarat { get; set; }
    public decimal Equivalent21 { get; set; }
    public decimal ManufacturingPerGram { get; set; }
    public decimal ImprovementPerGram { get; set; }
    public decimal TotalManufacturing { get; set; }
    public decimal TotalImprovement { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
