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
    public NotesViewModel Notes { get; }
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
    public RelayCommand ShowNotesCommand { get; }
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
        Notes = new NotesViewModel();
        Backup = new BackupViewModel();

        ShowDashboardCommand = new RelayCommand(_ => Navigate(Dashboard, L("NavDashboard")));
        ShowSuppliersCommand = new RelayCommand(_ => Navigate(Suppliers, L("NavSuppliers")));
        ShowTransactionsCommand = new RelayCommand(_ => Navigate(Transactions, L("NavTransactions")));
        ShowSupplierDetailsCommand = new RelayCommand(_ =>
        {
            SupplierDetails.LoadAll();
            Navigate(SupplierDetails, L("NavSupplierDetails"));
        });
        ShowWeeklyReportCommand = new RelayCommand(_ => Navigate(WeeklyReport, L("NavWeeklyReport")));
        ShowStatementCommand = new RelayCommand(_ => Navigate(Statement, L("NavStatement")));
        ShowNotesCommand = new RelayCommand(_ => Navigate(Notes, L("NavNotes")));
        ShowBackupCommand = new RelayCommand(_ => Navigate(Backup, L("NavBackup")));

        BackupCommand = new RelayCommand(_ => BackupDatabase());
        ToggleLanguageCommand = new RelayCommand(_ => ToggleLanguage());

        Suppliers.OpenDetailsRequested += supplier =>
        {
            SupplierDetails.LoadForSupplier(supplier);
            Navigate(SupplierDetails, L("NavSupplierDetails"));
        };

        Dashboard.OpenQuickAddSupplier += () =>
        {
            Navigate(Suppliers, L("NavSuppliers"));
            Suppliers.AddCommand.Execute(null);
        };
        Dashboard.OpenQuickAddTransaction += () =>
        {
            Navigate(Transactions, L("NavTransactions"));
            Transactions.AddCommand.Execute(null);
        };

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
                else
                {
                    details.LoadAll();
                }
                break;
            case TransactionsViewModel transactions:
                transactions.Load();
                break;
            case WeeklyReportViewModel report:
                report.Load();
                break;
            case NotesViewModel notes:
                notes.Load();
                break;
        }

        PageTitle = title;
        CurrentPage = page;
    }

    private static string L(string key) => UiText.L(key);

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
            case NotesViewModel: PageTitle = L("NavNotes"); break;
            case BackupViewModel: PageTitle = L("NavBackup"); break;
        }

        SupplierDetails.RefreshLocalization();
        Statement.GenerateCommand.Execute(null);
    }
}
