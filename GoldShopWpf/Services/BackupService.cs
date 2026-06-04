using System.IO;
using GoldShopCore;
using GoldShopCore.Data;
using GoldShopCore.Services;

namespace GoldShopWpf.Services;

public class BackupService
{
    private readonly string _backupDirectory;

    public BackupService()
    {
        _backupDirectory = AppStoragePaths.BackupDirectory;
    }

    public void EnsureAutomaticBackup()
    {
        try
        {
            Directory.CreateDirectory(_backupDirectory);
            var backupPath = Path.Combine(_backupDirectory, $"goldshop-auto-{DateTime.Today:yyyyMMdd}.db");
            if (!File.Exists(Database.DbFilePath) || File.Exists(backupPath))
            {
                return;
            }

            CreateBackupInternal(Database.DbFilePath, backupPath, "Automatic backup");
            PruneAutomaticBackups(14);
        }
        catch (Exception ex)
        {
            FileLogService.LogError("Automatic backup failed", ex);
        }
    }

    public void CreateManualBackup(string destinationPath)
    {
        CreateBackupInternal(Database.DbFilePath, destinationPath, "Manual backup");
    }

    public string BuildManualBackupFileName()
        => $"goldshop-backup-{DateTime.Now:yyyyMMdd}.db";

    public void RestoreBackup(string sourcePath)
    {
        Directory.CreateDirectory(_backupDirectory);
        var normalizedSourcePath = Path.GetFullPath(sourcePath);
        Database.ValidateDatabaseFileOrThrow(normalizedSourcePath, requireCoreTables: true);

        var restoreBackupPath = Path.Combine(_backupDirectory, $"goldshop-pre-restore-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        if (File.Exists(Database.DbFilePath))
        {
            CreateBackupInternal(Database.DbFilePath, restoreBackupPath, "Pre-restore snapshot");
        }

        var sourceInspection = Database.InspectDatabaseFile(normalizedSourcePath, requireCoreTables: true);
        FileLogService.LogInfo(
            "Database restore",
            $"RuntimeDatabasePath: {Database.DbFilePath}{Environment.NewLine}" +
            $"RestoreSourcePath: {normalizedSourcePath}{Environment.NewLine}" +
            $"RestoreSourceSizeBytes: {sourceInspection.FileSizeBytes}{Environment.NewLine}" +
            $"RestoreSourceSchemaVersion: {sourceInspection.SchemaVersion}{Environment.NewLine}" +
            $"RestoreSourceTransactions: {sourceInspection.TransactionCount}{Environment.NewLine}" +
            $"RestoreSourceDiscounts: {sourceInspection.DiscountCount}");

        Database.RestoreFromBackup(normalizedSourcePath, Database.DbFilePath);

        var restoredInspection = Database.InspectDatabaseFile(Database.DbFilePath, requireCoreTables: true);
        FileLogService.LogInfo(
            "Database restore",
            $"RestoreDestinationPath: {Database.DbFilePath}{Environment.NewLine}" +
            $"RestoreDestinationSizeBytes: {restoredInspection.FileSizeBytes}{Environment.NewLine}" +
            $"RestoreDestinationSchemaVersion: {restoredInspection.SchemaVersion}{Environment.NewLine}" +
            $"RestoreDestinationTransactions: {restoredInspection.TransactionCount}{Environment.NewLine}" +
            $"RestoreDestinationDiscounts: {restoredInspection.DiscountCount}");
    }

    public string BackupDirectory => _backupDirectory;

    private void PruneAutomaticBackups(int keepLatestCount)
    {
        var oldBackups = new DirectoryInfo(_backupDirectory)
            .GetFiles("goldshop-auto-*.db")
            .OrderByDescending(file => file.CreationTimeUtc)
            .Skip(keepLatestCount);

        foreach (var file in oldBackups)
        {
            file.Delete();
        }
    }

    private static void CreateBackupInternal(string sourceDbPath, string destinationPath, string context)
    {
        var normalizedDestinationPath = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(normalizedDestinationPath)!);

        Database.CreateConsistentBackup(sourceDbPath, normalizedDestinationPath);
        var inspection = Database.InspectDatabaseFile(normalizedDestinationPath, requireCoreTables: true);

        FileLogService.LogInfo(
            context,
            $"RuntimeDatabasePath: {Database.DbFilePath}{Environment.NewLine}" +
            $"BackupSourcePath: {Path.GetFullPath(sourceDbPath)}{Environment.NewLine}" +
            $"BackupDestinationPath: {normalizedDestinationPath}{Environment.NewLine}" +
            $"BackupSizeBytes: {inspection.FileSizeBytes}{Environment.NewLine}" +
            $"BackupSchemaVersion: {inspection.SchemaVersion}{Environment.NewLine}" +
            $"BackupTransactions: {inspection.TransactionCount}{Environment.NewLine}" +
            $"BackupDiscounts: {inspection.DiscountCount}");
    }
}
