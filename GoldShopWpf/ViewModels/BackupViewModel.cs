using System.IO;
using GoldShopCore.Data;

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
            Title = "Save Database Backup",
            Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
            FileName = $"goldshop-backup-{DateTime.Now:yyyyMMdd}.db"
        };

        if (dialog.ShowDialog() == true)
        {
            File.Copy(Database.DbFilePath, dialog.FileName, true);
            System.Windows.MessageBox.Show("Backup created.", "Backup", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
