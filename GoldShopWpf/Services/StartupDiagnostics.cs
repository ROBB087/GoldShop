using System.Reflection;
using GoldShopCore;
using GoldShopCore.Data;
using GoldShopCore.Services;

namespace GoldShopWpf.Services;

public static class StartupDiagnostics
{
    public static void LogStartupSnapshot()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        DatabaseInspectionResult schema;
        try
        {
            schema = Database.InspectDatabaseFile(Database.DbFilePath, requireCoreTables: false);
        }
        catch (Exception ex)
        {
            FileLogService.LogError("Startup diagnostics inspection failed", ex);
            schema = new DatabaseInspectionResult(Database.DbFilePath, false, 0, false, false, 0, 0, 0, 0, 0, 0, 0);
        }

        FileLogService.LogInfo(
            "Startup",
            $"Version: {version}{Environment.NewLine}" +
            $"Configuration: {AppRuntimePolicy.BuildConfigurationName}{Environment.NewLine}" +
            $"ExecutablePath: {AppRuntimePolicy.CurrentExecutablePath}{Environment.NewLine}" +
            $"ExecutableDirectory: {AppContext.BaseDirectory}{Environment.NewLine}" +
            $"InstallPath: {AppRuntimePolicy.OfficialInstallDirectory}{Environment.NewLine}" +
            $"OfficialExecutablePath: {AppRuntimePolicy.OfficialExecutablePath}{Environment.NewLine}" +
            $"RuntimeDataPath: {AppStoragePaths.RootDirectory}{Environment.NewLine}" +
            $"DatabaseDirectory: {AppStoragePaths.DataDirectory}{Environment.NewLine}" +
            $"DatabasePath: {Database.DbFilePath}{Environment.NewLine}" +
            $"DatabaseExists: {schema.Exists}{Environment.NewLine}" +
            $"DetectedSchemaVersion: {schema.SchemaVersion}{Environment.NewLine}" +
            $"DataPathOverrideActive: {AppStoragePaths.IsDevelopmentOverrideActive}");
    }
}
