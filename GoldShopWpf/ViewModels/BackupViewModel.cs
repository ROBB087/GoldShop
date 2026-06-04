using GoldShopWpf.Services;
using System.Windows;
using GoldShopCore.Data;
using GoldShopCore.Services;

namespace GoldShopWpf.ViewModels;

public class BackupViewModel : ViewModelBase
{
    public RelayCommand BackupCommand { get; }
    public RelayCommand RestoreCommand { get; }

    public BackupViewModel()
    {
        BackupCommand = new RelayCommand(_ => BackupDatabase());
        RestoreCommand = new RelayCommand(_ => RestoreDatabase());
    }

    private void BackupDatabase()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = UiText.L("MsgSaveBackupDialogTitle"),
            Filter = UiText.L("FilterSqlite"),
            FileName = $"goldshop-backup-{DateTime.Now:yyyyMMdd-HHmm}.db"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var runtimeInspection = Database.InspectDatabaseFile(Database.DbFilePath, requireCoreTables: false);
            FileLogService.LogInfo(
                "Backup UI flow",
                $"Action: BackupDialogConfirmed{Environment.NewLine}" +
                $"RuntimeDatabasePath: {Database.DbFilePath}{Environment.NewLine}" +
                $"SelectedBackupDestinationPath: {dialog.FileName}{Environment.NewLine}" +
                $"LiveTransactionsBeforeBackup: {runtimeInspection.TransactionCount}");
            AppServices.BackupService.CreateManualBackup(dialog.FileName);
            System.Windows.MessageBox.Show(UiText.L("MsgBackupCreated"), UiText.L("TitleBackup"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ExceptionReporter.ReportHandled(ex, "Backup creation failed");
            System.Windows.MessageBox.Show(
                UiText.LocalizeException(ex.Message),
                UiText.L("TitleBackup"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void RestoreDatabase()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = UiText.L("MsgRestoreBackupDialogTitle"),
            Filter = UiText.L("FilterBackup", UiText.L("FilterSqlite"))
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var confirm = System.Windows.MessageBox.Show(
            UiText.L("MsgRestoreConfirm"),
            UiText.L("TitleBackup"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var selectedInspection = Database.InspectDatabaseFile(dialog.FileName, requireCoreTables: false);
            FileLogService.LogInfo(
                "Backup UI flow",
                $"Action: RestoreDialogConfirmed{Environment.NewLine}" +
                $"RuntimeDatabasePath: {Database.DbFilePath}{Environment.NewLine}" +
                $"SelectedRestoreSourcePath: {dialog.FileName}{Environment.NewLine}" +
                $"TransactionsInsideSelectedRestoreFile: {selectedInspection.TransactionCount}");
            AppServices.RestoreDatabase(dialog.FileName);

            if (Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.ReloadAfterDatabaseRestore();
            }

            System.Windows.MessageBox.Show(UiText.L("MsgBackupRestored"), UiText.L("TitleBackup"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ExceptionReporter.ReportHandled(ex, "Restore backup failed");
            System.Windows.MessageBox.Show(
                UiText.LocalizeException(ex.Message),
                UiText.L("TitleBackup"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
