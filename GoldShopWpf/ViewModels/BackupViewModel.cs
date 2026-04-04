using GoldShopWpf.Services;

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

        AppServices.BackupService.CreateManualBackup(dialog.FileName);
        System.Windows.MessageBox.Show(UiText.L("MsgBackupCreated"), UiText.L("TitleBackup"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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

        AppServices.BackupService.RestoreBackup(dialog.FileName);
        System.Windows.MessageBox.Show(UiText.L("MsgBackupRestored"), UiText.L("TitleBackup"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
