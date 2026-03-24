namespace GoldShopCore.Models;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? WorkerName { get; set; }
    public string? WorkerPhone { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
