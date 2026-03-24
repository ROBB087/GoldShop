using System.Collections.ObjectModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class SupplierDetailsViewModel : ViewModelBase
{
    private SupplierListItem? _supplier;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private TransactionRow? _selectedTransaction;
    private SupplierListItem? _selectedSupplierOption;
    private decimal _totalGold21;
    private decimal _totalManufacturing;
    private decimal _totalImprovement;
    private decimal _manufacturingDiscounts;
    private decimal _improvementDiscounts;

    public ObservableCollection<SupplierListItem> SupplierOptions { get; } = new();
    public ObservableCollection<TransactionRow> Transactions { get; } = new();
    public ObservableCollection<DiscountListItem> Discounts { get; } = new();

    public SupplierListItem? SelectedSupplierOption
    {
        get => _selectedSupplierOption;
        set
        {
            if (SetProperty(ref _selectedSupplierOption, value))
            {
                Supplier = value;
                Load();
            }
        }
    }

    public SupplierListItem? Supplier
    {
        get => _supplier;
        set
        {
            if (SetProperty(ref _supplier, value))
            {
                AddTransactionCommand.RaiseCanExecuteChanged();
                AddDiscountCommand.RaiseCanExecuteChanged();
                EditTransactionCommand.RaiseCanExecuteChanged();
                DeleteTransactionCommand.RaiseCanExecuteChanged();
                PrintStatementCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(SupplierDisplayName));
            }
        }
    }

    public string SupplierDisplayName => Supplier?.Name ?? string.Empty;

    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public TransactionRow? SelectedTransaction
    {
        get => _selectedTransaction;
        set
        {
            if (SetProperty(ref _selectedTransaction, value))
            {
                EditTransactionCommand.RaiseCanExecuteChanged();
                DeleteTransactionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public decimal TotalGold21
    {
        get => _totalGold21;
        set => SetProperty(ref _totalGold21, value);
    }

    public decimal TotalManufacturing
    {
        get => _totalManufacturing;
        set
        {
            if (SetProperty(ref _totalManufacturing, value))
            {
                OnPropertyChanged(nameof(FinalManufacturing));
                OnPropertyChanged(nameof(NetManufacturing));
            }
        }
    }

    public decimal TotalImprovement
    {
        get => _totalImprovement;
        set
        {
            if (SetProperty(ref _totalImprovement, value))
            {
                OnPropertyChanged(nameof(FinalImprovement));
                OnPropertyChanged(nameof(NetImprovement));
            }
        }
    }

    public decimal ManufacturingDiscounts
    {
        get => _manufacturingDiscounts;
        set
        {
            if (SetProperty(ref _manufacturingDiscounts, value))
            {
                OnPropertyChanged(nameof(FinalManufacturing));
                OnPropertyChanged(nameof(NetManufacturing));
            }
        }
    }

    public decimal ImprovementDiscounts
    {
        get => _improvementDiscounts;
        set
        {
            if (SetProperty(ref _improvementDiscounts, value))
            {
                OnPropertyChanged(nameof(FinalImprovement));
                OnPropertyChanged(nameof(NetImprovement));
            }
        }
    }

    public decimal FinalManufacturing => TotalManufacturing - ManufacturingDiscounts;
    public decimal FinalImprovement => TotalImprovement - ImprovementDiscounts;
    public decimal NetManufacturing => FinalManufacturing;
    public decimal NetImprovement => FinalImprovement;

    public RelayCommand ApplyFilterCommand { get; }
    public RelayCommand ClearFilterCommand { get; }
    public RelayCommand AddTransactionCommand { get; }
    public RelayCommand AddDiscountCommand { get; }
    public RelayCommand EditTransactionCommand { get; }
    public RelayCommand DeleteTransactionCommand { get; }
    public RelayCommand PrintStatementCommand { get; }

    public SupplierDetailsViewModel()
    {
        ApplyFilterCommand = new RelayCommand(_ => Load());
        ClearFilterCommand = new RelayCommand(_ =>
        {
            FromDate = null;
            ToDate = null;
            Load();
        });
        AddTransactionCommand = new RelayCommand(_ => AddTransaction(), _ => Supplier != null);
        AddDiscountCommand = new RelayCommand(_ => AddDiscount(), _ => Supplier != null);
        EditTransactionCommand = new RelayCommand(_ => EditTransaction(), _ => Supplier != null && SelectedTransaction != null);
        DeleteTransactionCommand = new RelayCommand(_ => DeleteTransaction(), _ => Supplier != null && SelectedTransaction != null);
        PrintStatementCommand = new RelayCommand(_ => PrintStatement(), _ => Supplier != null);

        LoadSupplierOptions();
    }

    public void RefreshLocalization()
    {
        LoadSupplierOptions();
        SelectedSupplierOption = Supplier != null
            ? SupplierOptions.FirstOrDefault(s => s.Id == Supplier.Id)
            : SupplierOptions.FirstOrDefault();
    }

    public void LoadForSupplier(SupplierListItem supplier)
    {
        Supplier = supplier;
        LoadSupplierOptions();
        SelectedSupplierOption = SupplierOptions.FirstOrDefault(s => s.Id == supplier.Id);
        FromDate ??= DateTime.Today.AddMonths(-1);
        ToDate ??= DateTime.Today;
        Load();
    }

    public void LoadAll()
    {
        LoadSupplierOptions();
        Supplier = SupplierOptions.FirstOrDefault();
        SelectedSupplierOption = Supplier;
        FromDate ??= DateTime.Today.AddMonths(-1);
        ToDate ??= DateTime.Today;
        Load();
    }

    private void LoadSupplierOptions()
    {
        SupplierOptions.Clear();

        foreach (var supplier in AppServices.SupplierService.GetSuppliers())
        {
            SupplierOptions.Add(new SupplierListItem
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Phone = supplier.Phone ?? string.Empty,
                WorkerName = supplier.WorkerName ?? string.Empty,
                WorkerPhone = supplier.WorkerPhone ?? string.Empty
            });
        }

        SelectedSupplierOption ??= SupplierOptions.FirstOrDefault();
    }

    private void Load()
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            var msg = LocalizationService.CurrentLanguage == "ar"
                ? "تاريخ البداية يجب أن يكون قبل تاريخ النهاية"
                : "From date must be before To date.";
            System.Windows.MessageBox.Show(msg, "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        Transactions.Clear();
        Discounts.Clear();

        if (Supplier == null)
        {
            return;
        }

        foreach (var transaction in AppServices.TransactionService.GetTransactions(Supplier.Id, FromDate, ToDate))
        {
            Transactions.Add(new TransactionRow
            {
                Id = transaction.Id,
                SupplierName = Supplier.Name,
                Date = transaction.Date,
                Type = transaction.Type,
                OriginalWeight = transaction.OriginalWeight,
                OriginalKarat = transaction.OriginalKarat,
                Equivalent21 = transaction.Equivalent21,
                ManufacturingPerGram = transaction.ManufacturingPerGram,
                ImprovementPerGram = transaction.ImprovementPerGram,
                TotalManufacturing = transaction.TotalManufacturing,
                TotalImprovement = transaction.TotalImprovement,
                Traceability = transaction.Description ?? string.Empty,
                Notes = transaction.Notes ?? string.Empty,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            });
        }

        foreach (var discount in AppServices.DiscountService.GetDiscounts(Supplier.Id, FromDate, ToDate))
        {
            Discounts.Add(new DiscountListItem
            {
                Id = discount.Id,
                Type = discount.Type,
                Amount = discount.Amount,
                Notes = discount.Notes ?? string.Empty,
                CreatedAt = discount.CreatedAt
            });
        }

        ApplySummary(AppServices.TransactionService.GetSummary(Supplier.Id, FromDate, ToDate));
    }

    private void AddTransaction()
    {
        if (Supplier == null)
        {
            return;
        }

        var dialog = new Views.TransactionWindow(Supplier.Id, SupplierOptions);
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            AppServices.TransactionService.AddTransaction(
                Supplier.Id,
                dialog.TransactionDate,
                dialog.TransactionType,
                dialog.OriginalWeight,
                dialog.OriginalKarat,
                dialog.ManufacturingPerGram,
                dialog.ImprovementPerGram,
                dialog.Notes);
            Load();
        }
        catch (ArgumentException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private void AddDiscount()
    {
        if (Supplier == null)
        {
            return;
        }

        var dialog = new Views.DiscountWindow();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            AppServices.DiscountService.AddDiscount(Supplier.Id, dialog.DiscountType, dialog.Amount, dialog.Notes);
            Load();
        }
        catch (ArgumentException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private void EditTransaction()
    {
        if (Supplier == null || SelectedTransaction == null)
        {
            return;
        }

        var transaction = Transactions.FirstOrDefault(t => t.Id == SelectedTransaction.Id);
        if (transaction == null)
        {
            return;
        }

        var item = new TransactionListItem
        {
            Id = transaction.Id,
            SupplierId = Supplier.Id,
            SupplierName = Supplier.Name,
            Date = transaction.Date,
            Type = transaction.Type,
            OriginalWeight = transaction.OriginalWeight,
            OriginalKarat = transaction.OriginalKarat,
            Equivalent21 = transaction.Equivalent21,
            ManufacturingPerGram = transaction.ManufacturingPerGram,
            ImprovementPerGram = transaction.ImprovementPerGram,
            TotalManufacturing = transaction.TotalManufacturing,
            TotalImprovement = transaction.TotalImprovement,
            Traceability = transaction.Traceability,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };

        var dialog = new Views.TransactionWindow(item, SupplierOptions);
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            AppServices.TransactionService.UpdateTransaction(
                SelectedTransaction.Id,
                Supplier.Id,
                dialog.TransactionDate,
                dialog.TransactionType,
                dialog.OriginalWeight,
                dialog.OriginalKarat,
                dialog.ManufacturingPerGram,
                dialog.ImprovementPerGram,
                dialog.Notes);
            Load();
        }
        catch (ArgumentException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private void DeleteTransaction()
    {
        if (SelectedTransaction == null)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            "Delete selected transaction?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            AppServices.TransactionService.DeleteTransaction(SelectedTransaction.Id);
            Load();
        }
    }

    private void PrintStatement()
    {
        if (Supplier == null)
        {
            return;
        }

        var statementWindow = new Views.StatementWindow(Supplier.Id, Supplier.Name, FromDate, ToDate);
        statementWindow.ShowDialog();
    }

    private void ApplySummary(TraderSummary summary)
    {
        TotalGold21 = summary.TotalGold21;
        TotalManufacturing = summary.TotalManufacturing;
        TotalImprovement = summary.TotalImprovement;
        ManufacturingDiscounts = summary.ManufacturingDiscounts;
        ImprovementDiscounts = summary.ImprovementDiscounts;
    }
}
