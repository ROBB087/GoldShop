using System.IO;
using GoldShopCore.Data;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentPage = null!;
    private string _pageTitle = "Dashboard";

    public DashboardViewModel Dashboard { get; }
    public SuppliersViewModel Suppliers { get; }
    public SupplierDetailsViewModel SupplierDetails { get; }
    public TransactionsViewModel Transactions { get; }
    public WeeklyReportViewModel WeeklyReport { get; }
    public StatementViewModel Statement { get; }
    public BackupViewModel Backup { get; }

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public string PageTitle
    {
        get => _pageTitle;
        set => SetProperty(ref _pageTitle, value);
    }

    public RelayCommand ShowDashboardCommand { get; }
    public RelayCommand ShowSuppliersCommand { get; }
    public RelayCommand ShowTransactionsCommand { get; }
    public RelayCommand ShowSupplierDetailsCommand { get; }
    public RelayCommand ShowWeeklyReportCommand { get; }
    public RelayCommand ShowStatementCommand { get; }
    public RelayCommand ShowBackupCommand { get; }
    public RelayCommand BackupCommand { get; }
    public RelayCommand ToggleLanguageCommand { get; }

    public MainViewModel()
    {
        Dashboard = new DashboardViewModel();
        Suppliers = new SuppliersViewModel();
        SupplierDetails = new SupplierDetailsViewModel();
        Transactions = new TransactionsViewModel();
        WeeklyReport = new WeeklyReportViewModel();
        Statement = new StatementViewModel();
        Backup = new BackupViewModel();

        ShowDashboardCommand = new RelayCommand(_ => Navigate(Dashboard, L("NavDashboard")));
        ShowSuppliersCommand = new RelayCommand(_ => Navigate(Suppliers, L("NavSuppliers")));
        ShowTransactionsCommand = new RelayCommand(_ => Navigate(Transactions, L("NavTransactions")));
        ShowSupplierDetailsCommand = new RelayCommand(_ => Navigate(SupplierDetails, L("NavSupplierDetails")));
        ShowWeeklyReportCommand = new RelayCommand(_ => Navigate(WeeklyReport, L("NavWeeklyReport")));
        ShowStatementCommand = new RelayCommand(_ => Navigate(Statement, L("NavStatement")));
        ShowBackupCommand = new RelayCommand(_ => Navigate(Backup, L("NavBackup")));

        BackupCommand = new RelayCommand(_ => BackupDatabase());
        ToggleLanguageCommand = new RelayCommand(_ => ToggleLanguage());

        Suppliers.OpenDetailsRequested += supplier =>
        {
            SupplierDetails.LoadForSupplier(supplier);
            Navigate(SupplierDetails, "Supplier Details");
        };

        Dashboard.OpenQuickAddSupplier += () => Suppliers.AddCommand.Execute(null);
        Dashboard.OpenQuickAddTransaction += () => Transactions.AddCommand.Execute(null);

        CurrentPage = Dashboard;
        PageTitle = L("NavDashboard");
    }

    private void Navigate(ViewModelBase page, string title)
    {
        switch (page)
        {
            case DashboardViewModel dashboard:
                dashboard.Load();
                break;
            case SuppliersViewModel suppliers:
                suppliers.Load();
                break;
            case SupplierDetailsViewModel details:
                if (details.Supplier != null)
                {
                    details.LoadForSupplier(details.Supplier);
                }
                break;
            case TransactionsViewModel transactions:
                transactions.Load();
                break;
            case WeeklyReportViewModel report:
                report.Load();
                break;
        }

        PageTitle = title;
        CurrentPage = page;
    }

    private static string L(string key)
    {
        return System.Windows.Application.Current.TryFindResource(key)?.ToString() ?? key;
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

    private void ToggleLanguage()
    {
        var next = LocalizationService.CurrentLanguage == "ar" ? "en" : "ar";
        LocalizationService.SetLanguage(next);
        // refresh title to match language
        switch (CurrentPage)
        {
            case DashboardViewModel: PageTitle = L("NavDashboard"); break;
            case SuppliersViewModel: PageTitle = L("NavSuppliers"); break;
            case SupplierDetailsViewModel: PageTitle = L("NavSupplierDetails"); break;
            case TransactionsViewModel: PageTitle = L("NavTransactions"); break;
            case WeeklyReportViewModel: PageTitle = L("NavWeeklyReport"); break;
            case StatementViewModel: PageTitle = L("NavStatement"); break;
            case BackupViewModel: PageTitle = L("NavBackup"); break;
        }

        SupplierDetails.RefreshLocalization();
        Statement.GenerateCommand.Execute(null);
    }
}
