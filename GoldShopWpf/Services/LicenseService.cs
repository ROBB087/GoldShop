using System.IO;
using System.Windows;
using GoldShopCore.Models;
using GoldShopCore.Services;
using GoldShopWpf.Views;

namespace GoldShopWpf.Services;

public static class LicenseService
{
    private static readonly InstalledLicenseStore InstalledLicenseStore = new();
    private static readonly UsedTokenStore UsedTokenStore = new();

    public static InstalledLicenseRecord? CurrentLicense { get; private set; }

    public static string GetMachineId() => MachineFingerprintService.GetCurrentMachineId();

    public static bool EnsureActivated()
    {
        var machineId = GetMachineId();
        var status = InspectActivationState(machineId, out var license, out var error);

        if (status == LicenseStartupStatus.Valid)
        {
            CurrentLicense = license;
            return true;
        }

        if (status == LicenseStartupStatus.RequiresActivation)
        {
            var activationWindow = new ActivationWindow();
            var result = activationWindow.ShowDialog();
            return result == true && CurrentLicense != null;
        }

        MessageBox.Show(error, "التفعيل", MessageBoxButton.OK, MessageBoxImage.Stop);
        FileLogService.LogWarning("License startup", error);
        return false;
    }

    public static bool TryActivate(string activationToken, out string error)
    {
        error = string.Empty;
        var normalizedToken = activationToken.Trim();
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            error = "أدخل رمز التفعيل أولاً.";
            return false;
        }

        var machineId = GetMachineId();
        var tokenHash = TokenHasher.HashToken(normalizedToken);
        if (!ValidTokenCatalog.ContainsHash(tokenHash))
        {
            error = "رمز التفعيل غير صالح.";
            FileLogService.LogWarning("License activation", error);
            return false;
        }

        UsedTokenRegistry registry;
        try
        {
            registry = UsedTokenStore.Load(machineId);
        }
        catch (Exception ex)
        {
            FileLogService.LogError("Failed to load used-token registry.", ex);
            error = "تعذر قراءة سجل التفعيل المحلي.";
            return false;
        }

        if (registry.TokenHashes.Contains(tokenHash, StringComparer.Ordinal))
        {
            error = "تم استخدام رمز التفعيل هذا من قبل ولا يمكن إعادة استخدامه.";
            FileLogService.LogWarning("License activation", error);
            return false;
        }

        var updatedRegistry = new UsedTokenRegistry
        {
            ProductCode = LicenseConstants.ProductCode,
            TokenHashes = [.. registry.TokenHashes, tokenHash]
        };

        var installedLicense = new InstalledLicenseRecord
        {
            TokenHash = tokenHash,
            MachineId = machineId,
            ProductCode = LicenseConstants.ProductCode,
            ActivationDateUtc = DateTime.UtcNow
        };

        try
        {
            UsedTokenStore.Save(updatedRegistry, machineId);
            InstalledLicenseStore.Save(installedLicense, machineId);
            CurrentLicense = installedLicense;
            FileLogService.LogInfo("License activation", $"Activation succeeded for machine {machineId}.");
            return true;
        }
        catch (Exception ex)
        {
            FileLogService.LogError("Failed to persist offline activation.", ex);
            error = "تعذر حفظ بيانات التفعيل بشكل آمن.";
            return false;
        }
    }

    private static LicenseStartupStatus InspectActivationState(string machineId, out InstalledLicenseRecord? license, out string error)
    {
        license = null;
        error = string.Empty;

        var hasLicenseFile = File.Exists(LicensePathProvider.LicenseFilePath);
        var hasUsedTokensFile = File.Exists(LicensePathProvider.UsedTokensFilePath);

        if (!hasLicenseFile && !hasUsedTokensFile)
        {
            return LicenseStartupStatus.RequiresActivation;
        }

        if (!hasLicenseFile || !hasUsedTokensFile)
        {
            error = "تم اكتشاف عبث بملفات التفعيل المحلية.";
            return LicenseStartupStatus.Tampered;
        }

        try
        {
            var registry = UsedTokenStore.Load(machineId);
            license = InstalledLicenseStore.Load(machineId);

            if (!string.Equals(license.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
            {
                error = "ملف الترخيص لا يخص هذا الجهاز.";
                license = null;
                return LicenseStartupStatus.Tampered;
            }

            if (string.IsNullOrWhiteSpace(license.TokenHash) ||
                !ValidTokenCatalog.ContainsHash(license.TokenHash) ||
                !registry.TokenHashes.Contains(license.TokenHash, StringComparer.Ordinal))
            {
                error = "تم اكتشاف عبث ببيانات التفعيل المحلية.";
                license = null;
                return LicenseStartupStatus.Tampered;
            }

            return LicenseStartupStatus.Valid;
        }
        catch (Exception ex)
        {
            FileLogService.LogError("Offline license startup validation failed.", ex);
            error = "تعذر قراءة ملفات التفعيل أو تم اكتشاف محاولة عبث.";
            return LicenseStartupStatus.Tampered;
        }
    }

    private enum LicenseStartupStatus
    {
        Valid,
        RequiresActivation,
        Tampered
    }
}
