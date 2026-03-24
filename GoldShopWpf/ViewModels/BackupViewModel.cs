using System.IO;
using GoldShopCore.Data;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class BackupViewModel : ViewModelBase
{
    public RelayCommand BackupCommand { get; }

    public BackupViewModel()
    {
        BackupCommand = new RelayCommand(_ => BackupDatabase());
    }

    private void BackupDatabase()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = UiText.L("MsgSaveBackupDialogTitle"),
            Filter = UiText.L("FilterSqlite"),
            FileName = $"goldshop-backup-{DateTime.Now:yyyyMMdd}.db"
        };

        if (dialog.ShowDialog() == true)
        {
            File.Copy(Database.DbFilePath, dialog.FileName, true);
            System.Windows.MessageBox.Show(UiText.L("MsgBackupCreated"), UiText.L("TitleBackup"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
