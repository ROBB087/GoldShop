namespace GoldShopCore.Models;

public sealed class InstalledLicenseRecord
{
    public string TokenHash { get; init; } = string.Empty;
    public string MachineId { get; init; } = string.Empty;
    public string ProductCode { get; init; } = string.Empty;
    public DateTime ActivationDateUtc { get; init; } = DateTime.UtcNow;
}
