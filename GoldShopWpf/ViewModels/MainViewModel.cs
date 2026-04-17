using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentPage = null!;
    private string _pageTitle = "Dashboard";
    private string _activePageKey = "Dashboard";

    public System.Collections.ObjectModel.ObservableCollection<ToastMessageViewModel> Toasts => ToastService.Messages;
    public DashboardViewModel Dashboard { get; }
    public SuppliersViewModel Suppliers { get; }
    public SupplierDetailsViewModel SupplierDetails { get; }
    public TransactionsViewModel Transactions { get; }
    public StatementViewModel Statement { get; }
    public NotesViewModel Notes { get; }
    public PricingSettingsViewModel PricingSettings { get; }
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

    public string ActivePageKey
    {
        get => _activePageKey;
        set => SetProperty(ref _activePageKey, value);
    }

    public RelayCommand ShowDashboardCommand { get; }
    public RelayCommand ShowSuppliersCommand { get; }
    public RelayCommand ShowTransactionsCommand { get; }
    public RelayCommand ShowSupplierDetailsCommand { get; }
    public RelayCommand ShowStatementCommand { get; }
    public RelayCommand ShowNotesCommand { get; }
    public RelayCommand ShowPricingSettingsCommand { get; }
    public RelayCommand ShowBackupCommand { get; }
    public RelayCommand BackupCommand { get; }
    public RelayCommand ToggleLanguageCommand { get; }

    public MainViewModel()
    {
        Dashboard = new DashboardViewModel();
        Suppliers = new SuppliersViewModel();
        SupplierDetails = new SupplierDetailsViewModel();
        Transactions = new TransactionsViewModel();
        Statement = new StatementViewModel();
        Notes = new NotesViewModel();
        PricingSettings = new PricingSettingsViewModel();
        Backup = new BackupViewModel();

        ShowDashboardCommand = new RelayCommand(_ => Navigate(Dashboard, L("NavDashboard"), "Dashboard"));
        ShowSuppliersCommand = new RelayCommand(_ => Navigate(Suppliers, L("NavSuppliers"), "Suppliers"));
        ShowTransactionsCommand = new RelayCommand(_ => Navigate(Transactions, L("NavTransactions"), "Transactions"));
        ShowSupplierDetailsCommand = new RelayCommand(_ =>
        {
            SupplierDetails.LoadAll();
            Navigate(SupplierDetails, L("NavSupplierDetails"), "SupplierDetails");
        });
        ShowStatementCommand = new RelayCommand(_ => Navigate(Statement, L("NavStatement"), "Statement"));
        ShowNotesCommand = new RelayCommand(_ => Navigate(Notes, L("NavNotes"), "Notes"));
        ShowPricingSettingsCommand = new RelayCommand(_ => Navigate(PricingSettings, L("NavPricingSettings"), "PricingSettings"));
        ShowBackupCommand = new RelayCommand(_ => Navigate(Backup, L("NavBackup"), "Backup"));

        BackupCommand = new RelayCommand(_ => BackupDatabase());
        ToggleLanguageCommand = new RelayCommand(_ => ToggleLanguage());

        Suppliers.OpenDetailsRequested += supplier =>
        {
            SupplierDetails.LoadForSupplier(supplier);
            Navigate(SupplierDetails, L("NavSupplierDetails"), "SupplierDetails");
        };

        Dashboard.OpenQuickAddSupplier += () =>
        {
            Navigate(Suppliers, L("NavSuppliers"), "Suppliers");
            Suppliers.AddCommand.Execute(null);
        };
        Dashboard.OpenQuickAddTransaction += () =>
        {
            Navigate(Transactions, L("NavTransactions"), "Transactions");
            Transactions.AddCommand.Execute(null);
        };

        CurrentPage = Dashboard;
        PageTitle = L("NavDashboard");
        ActivePageKey = "Dashboard";
    }

    private void Navigate(ViewModelBase page, string title, string activePageKey)
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
            case NotesViewModel notes:
                notes.Load();
                break;
            case PricingSettingsViewModel pricingSettings:
                pricingSettings.Load();
                break;
        }

        PageTitle = title;
        ActivePageKey = activePageKey;
        CurrentPage = page;
    }

    private static string L(string key) => UiText.L(key);

    public void ReloadAfterDatabaseRestore()
    {
        Dashboard.Load();
        Suppliers.Load();
        SupplierDetails.LoadAll();
        Transactions.RefreshLocalization();
        Notes.Load();
        PricingSettings.Load();
        Statement.ReloadData();
    }

    private void BackupDatabase()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = UiText.L("MsgSaveBackupDialogTitle"),
            Filter = UiText.L("FilterSqlite"),
            FileName = AppServices.BackupService.BuildManualBackupFileName()
        };

        if (dialog.ShowDialog() == true)
        {
            AppServices.BackupService.CreateManualBackup(dialog.FileName);
            ToastService.ShowSuccess(UiText.L("MsgBackupCreated"));
        }
    }

    private void ToggleLanguage()
    {
        var next = LocalizationService.CurrentLanguage == "ar" ? "en" : "ar";
        LocalizationService.SetLanguage(next);
        switch (CurrentPage)
        {
            case DashboardViewModel: PageTitle = L("NavDashboard"); break;
            case SuppliersViewModel: PageTitle = L("NavSuppliers"); break;
            case SupplierDetailsViewModel: PageTitle = L("NavSupplierDetails"); break;
            case TransactionsViewModel: PageTitle = L("NavTransactions"); break;
            case StatementViewModel: PageTitle = L("NavStatement"); break;
            case NotesViewModel: PageTitle = L("NavNotes"); break;
            case PricingSettingsViewModel: PageTitle = L("NavPricingSettings"); break;
            case BackupViewModel: PageTitle = L("NavBackup"); break;
        }

        SupplierDetails.RefreshLocalization();
        Dashboard.Load();
        Suppliers.Load();
        Transactions.RefreshLocalization();
        Notes.Load();
        PricingSettings.Load();
        Statement.GenerateCommand.Execute(null);
    }
}
