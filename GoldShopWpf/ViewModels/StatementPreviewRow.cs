namespace GoldShopWpf.ViewModels;

public class StatementPreviewRow
{
    public DateTime Date { get; init; }
    public string Type { get; init; } = string.Empty;
    public decimal Weight { get; init; }
    public string Item { get; init; } = string.Empty;
    public decimal Value { get; init; }
}
