using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace GoldShopWpf.Services;

internal static class MachineFingerprintService
{
    public static string GetCurrentMachineId()
    {
        var parts = new[]
        {
            GetMachineGuid(),
            QueryWmiValue("Win32_Processor", "ProcessorId"),
            QueryWmiValue("Win32_DiskDrive", "SerialNumber"),
            QueryWmiValue("Win32_BIOS", "SerialNumber"),
            Environment.MachineName
        }
        .Select(Normalize)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase);

        var combined = string.Join("|", parts);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{LicenseConstants.ProductCode}|{combined}"));
        var hex = Convert.ToHexString(hash);

        return string.Join("-",
            hex[..5],
            hex[5..10],
            hex[10..15],
            hex[15..20],
            hex[20..25],
            hex[25..30]);
    }

    private static string GetMachineGuid()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var machineKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                var machineGuid = machineKey?.GetValue("MachineGuid")?.ToString();
                if (!string.IsNullOrWhiteSpace(machineGuid))
                {
                    return machineGuid;
                }
            }
            catch
            {
            }
        }

        return $"{Environment.MachineName}|{Environment.OSVersion.VersionString}|{Environment.ProcessorCount}";
    }

    private static string QueryWmiValue(string className, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
            using var results = searcher.Get();
            foreach (var item in results)
            {
                if (item is ManagementObject managementObject)
                {
                    var value = managementObject[propertyName]?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private static string Normalize(string value)
        => value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
