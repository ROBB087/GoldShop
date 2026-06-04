using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace GoldShopWpf.Services;

public static class AppRuntimePolicy
{
    private const string SupportOverrideEnvVar = "GOLDSHOP_ALLOW_UNOFFICIAL_RUN";

    public static string OfficialInstallDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "GoldShop");

    public static string OfficialExecutablePath =>
        Path.Combine(OfficialInstallDirectory, "GoldShop.exe");

    public static bool IsProductionBuild
    {
        get
        {
#if DEBUG
            return false;
#else
            return true;
#endif
        }
    }

    public static bool AllowUnofficialRun =>
        string.Equals(Environment.GetEnvironmentVariable(SupportOverrideEnvVar), "1", StringComparison.OrdinalIgnoreCase);

    public static string CurrentExecutablePath =>
        Environment.ProcessPath
        ?? Process.GetCurrentProcess().MainModule?.FileName
        ?? Assembly.GetEntryAssembly()?.Location
        ?? AppContext.BaseDirectory;

    public static bool IsRunningFromOfficialInstallLocation()
    {
        var currentPath = NormalizePath(CurrentExecutablePath);
        var officialPath = NormalizePath(OfficialExecutablePath);
        return string.Equals(currentPath, officialPath, StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildConfigurationName => IsProductionBuild ? "Release" : "Debug";

    private static string NormalizePath(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
}
