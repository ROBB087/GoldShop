using System.IO;

namespace GoldShopWpf.Services;

internal static class LicensePathProvider
{
    private static string SecurityDirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GoldShop", "Security");

    public static string LicenseFilePath => Path.Combine(SecurityDirectoryPath, "license.bin");

    public static string UsedTokensFilePath => Path.Combine(SecurityDirectoryPath, "used-tokens.bin");

    public static void EnsureDirectoryExists() => Directory.CreateDirectory(SecurityDirectoryPath);
}
