namespace GoldShopCore.Models;

public class TraderSummarySnapshot
{
    public int TraderId { get; set; }
    public decimal TotalEquivalent21 { get; set; }
    public decimal TotalManufacturing { get; set; }
    public decimal TotalImprovement { get; set; }
    public decimal ManufacturingDiscounts { get; set; }
    public decimal ImprovementDiscounts { get; set; }
    public decimal TotalDiscounts => ManufacturingDiscounts + ImprovementDiscounts;
    public decimal NetManufacturing => TotalManufacturing - ManufacturingDiscounts;
    public decimal NetImprovement => TotalImprovement - ImprovementDiscounts;
    public decimal NetValues => NetManufacturing + NetImprovement;
    public DateTime LastUpdated { get; set; }
}
