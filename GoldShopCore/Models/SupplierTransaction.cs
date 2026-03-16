namespace GoldShopCore.Models;

public enum TransactionType
{
    GoldGiven = 1,
    GoldReceived = 2,
    PaymentIssued = 3,
    PaymentReceived = 4
}

public enum TransactionCategory
{
    None = 0,
    Internal = 1,
    External = 2
}

public class SupplierTransaction
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal? Weight { get; set; }
    public string? Purity { get; set; }
    public TransactionCategory Category { get; set; } = TransactionCategory.None;
    public string? Notes { get; set; }
}
