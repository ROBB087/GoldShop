using System.IO;
using GoldShopCore.Data;

namespace GoldShopWpf.Services;

public class BackupService
{
    private readonly string _backupDirectory;

    public BackupService()
    {
        _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
    }

    public void EnsureAutomaticBackup()
    {
        Directory.CreateDirectory(_backupDirectory);
        var backupPath = Path.Combine(_backupDirectory, $"goldshop-auto-{DateTime.Today:yyyyMMdd}.db");
        if (!File.Exists(Database.DbFilePath) || File.Exists(backupPath))
        {
            return;
        }

        File.Copy(Database.DbFilePath, backupPath, overwrite: false);
        PruneAutomaticBackups(14);
    }

    public void CreateManualBackup(string destinationPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(Database.DbFilePath, destinationPath, true);
    }

    public void RestoreBackup(string sourcePath)
    {
        var restoreBackupPath = Path.Combine(_backupDirectory, $"goldshop-pre-restore-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        if (File.Exists(Database.DbFilePath))
        {
            File.Copy(Database.DbFilePath, restoreBackupPath, true);
        }

        File.Copy(sourcePath, Database.DbFilePath, true);
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
}
