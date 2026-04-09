namespace GoldShopCore.Models;

public sealed class UsedTokenRegistry
{
    public string ProductCode { get; init; } = string.Empty;
    public List<string> TokenHashes { get; init; } = [];
}
