namespace GoldShopCore.Models;

public sealed class AppLicense
{
    public string LicensedTo { get; init; } = string.Empty;
    public string MachineId { get; init; } = string.Empty;
    public string ProductCode { get; init; } = string.Empty;
    public DateTime IssuedAtUtc { get; init; } = DateTime.UtcNow;
}
