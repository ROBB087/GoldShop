namespace GoldShopCore.Models;

public class PricingSettings
{
    public int Id { get; set; }
    public decimal DefaultManufacturingPerGram { get; set; }
    public decimal DefaultManufacturingPerGram24 { get; set; }
    public decimal DefaultImprovementPerGram { get; set; }
    public DateTime CreatedAt { get; set; }
}
