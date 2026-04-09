using System.IO;
using GoldShopCore.Models;

namespace GoldShopWpf.Services;

internal sealed class InstalledLicenseStore
{
    private const string Purpose = "InstalledLicenseStore";

    public InstalledLicenseRecord Load(string machineId)
    {
        var payload = File.ReadAllBytes(LicensePathProvider.LicenseFilePath);
        var record = LicenseCryptoService.Decrypt<InstalledLicenseRecord>(payload, Purpose, machineId);
        return record.ProductCode == LicenseConstants.ProductCode
            ? record
            : throw new InvalidDataException("Installed license product code is invalid.");
    }

    public void Save(InstalledLicenseRecord record, string machineId)
    {
        LicensePathProvider.EnsureDirectoryExists();
        var payload = LicenseCryptoService.Encrypt(record, Purpose, machineId);
        WriteAtomic(LicensePathProvider.LicenseFilePath, payload);
    }

    private static void WriteAtomic(string path, byte[] payload)
    {
        var tempPath = $"{path}.tmp";
        File.WriteAllBytes(tempPath, payload);
        File.Move(tempPath, path, true);
    }
}
