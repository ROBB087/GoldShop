using System.IO;
using GoldShopCore.Models;

namespace GoldShopWpf.Services;

internal sealed class UsedTokenStore
{
    private const string Purpose = "UsedTokenStore";

    public UsedTokenRegistry Load(string machineId)
    {
        if (!File.Exists(LicensePathProvider.UsedTokensFilePath))
        {
            return CreateEmpty();
        }

        var payload = File.ReadAllBytes(LicensePathProvider.UsedTokensFilePath);
        var registry = LicenseCryptoService.Decrypt<UsedTokenRegistry>(payload, Purpose, machineId);
        return registry.ProductCode == LicenseConstants.ProductCode
            ? registry
            : throw new InvalidDataException("Used token store product code is invalid.");
    }

    public void Save(UsedTokenRegistry registry, string machineId)
    {
        LicensePathProvider.EnsureDirectoryExists();
        var payload = LicenseCryptoService.Encrypt(registry, Purpose, machineId);
        WriteAtomic(LicensePathProvider.UsedTokensFilePath, payload);
    }

    public static UsedTokenRegistry CreateEmpty()
        => new()
        {
            ProductCode = LicenseConstants.ProductCode,
            TokenHashes = []
        };

    private static void WriteAtomic(string path, byte[] payload)
    {
        var tempPath = $"{path}.tmp";
        File.WriteAllBytes(tempPath, payload);
        File.Move(tempPath, path, true);
    }
}
