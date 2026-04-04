using System.IO;
using System.Security.Cryptography;
using System.Text;
using GoldShopCore.Models;
using GoldShopCore.Services;
using GoldShopWpf.Views;
using Microsoft.Win32;
using System.Windows;

namespace GoldShopWpf.Services;

public static class LicenseService
{
    private const string ProductCode = "GoldShopWpf";
    private const string PublicKeyXml = "<RSAKeyValue><Modulus>xtOrITHZZh0mXFtzge9oM2LjDKoJ7vThphUt2MqODyFPhiCb5P4hmpFX3Fux7QQpqupTF2ffVJXJiq3vh51UT6fYwaocvt9qtFmFWRskjgJNhEJfcE8ymEBSQehgT9QgRdRxgmQUcI2470liPSHvjP/pFgl6WYVFDlumEVcIxrlWTy+DHNkKcyUFDrO6PnCtBGGNxqyJ+tK1kv8WeDkIcG4BF5pC97xBzigOtozxwdH7niIwtU8yNcelNm17P8drHwbjkxrvhT1LPl9NwSGZgajWGvAufmn1SK1nVDCwXcT8NDsYz8vB3rpnifPet9Sa2pQU2e7z4sSJlW4yM1xl2Q==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("GoldShop::License");

    public static AppLicense? CurrentLicense { get; private set; }
    public static string LicensedTo => CurrentLicense?.LicensedTo ?? "Unlicensed";

    public static string GetMachineId()
    {
        var rawMachineValue = GetMachineGuid();
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes($"{rawMachineValue}|{ProductCode}"));
        var hex = Convert.ToHexString(hash);
        return string.Join("-",
            hex[..5],
            hex[5..10],
            hex[10..15],
            hex[15..20],
            hex[20..25]);
    }

    public static bool EnsureActivated()
    {
        if (TryLoadStoredLicense(out var license, out _))
        {
            CurrentLicense = license;
            return true;
        }

        var activationWindow = new ActivationWindow();

        var result = activationWindow.ShowDialog();
        return result == true && CurrentLicense != null;
    }

    public static bool TryActivate(string activationKey, out string error)
    {
        if (!TryValidateKey(activationKey, out var license, out error))
        {
            return false;
        }

        SaveLicenseKey(activationKey.Trim());
        CurrentLicense = license;
        return true;
    }

    public static bool TryValidateKey(string activationKey, out AppLicense? license, out string error)
    {
        if (!LicenseKeyCodec.TryReadKey(activationKey, PublicKeyXml, out license, out error))
        {
            error = TranslateError(error);
            return false;
        }

        if (!string.Equals(license!.ProductCode, ProductCode, StringComparison.Ordinal))
        {
            error = "هذا المفتاح لا يخص هذا البرنامج.";
            license = null;
            return false;
        }

        var machineId = GetMachineId();
        if (!string.Equals(license.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
        {
            error = "هذا المفتاح مرتبط بجهاز آخر.";
            license = null;
            return false;
        }

        return true;
    }

    private static bool TryLoadStoredLicense(out AppLicense? license, out string error)
    {
        license = null;
        error = string.Empty;

        if (!File.Exists(LicenseFilePath))
        {
            error = "License file not found.";
            return false;
        }

        try
        {
            var encrypted = File.ReadAllBytes(LicenseFilePath);
            var rawKeyBytes = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.LocalMachine);
            var rawKey = Encoding.UTF8.GetString(rawKeyBytes);
            return TryValidateKey(rawKey, out license, out error);
        }
        catch (Exception ex)
        {
            FileLogService.LogError("Failed to load license file.", ex);
            error = "تعذر قراءة ملف الترخيص.";
            return false;
        }
    }

    private static void SaveLicenseKey(string activationKey)
    {
        Directory.CreateDirectory(LicenseDirectoryPath);
        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(activationKey), Entropy, DataProtectionScope.LocalMachine);
        File.WriteAllBytes(LicenseFilePath, encrypted);
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

    private static string TranslateError(string error)
    {
        return error switch
        {
            "License key is empty." => "أدخل مفتاح الترخيص أولاً.",
            "License key format is invalid." => "صيغة مفتاح الترخيص غير صحيحة.",
            "License signature is invalid." => "توقيع مفتاح الترخيص غير صالح.",
            "License payload is invalid." => "بيانات مفتاح الترخيص غير صالحة.",
            _ => error
        };
    }

    private static string LicenseDirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GoldShop");

    private static string LicenseFilePath => Path.Combine(LicenseDirectoryPath, "license.dat");
}
