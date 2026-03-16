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
    private decimal _currentBalance;
    private decimal _totalDebit;
    private decimal _totalCredit;
    private SupplierListItem? _selectedSupplierOption;

    public ObservableCollection<SupplierListItem> SupplierOptions { get; } = new();

    public SupplierListItem? SelectedSupplierOption
    {
        get => _selectedSupplierOption;
        set
        {
            if (SetProperty(ref _selectedSupplierOption, value))
            {
                Supplier = value?.Id == 0 ? null : value;
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
                AddGoldCommand.RaiseCanExecuteChanged();
                AddPaymentCommand.RaiseCanExecuteChanged();
                PrintStatementCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(SupplierDisplayName));
            }
        }
    }

    public string SupplierDisplayName
        => Supplier?.Name ?? (System.Windows.Application.Current.TryFindResource("LblAllSuppliers")?.ToString() ?? string.Empty);

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

    public decimal CurrentBalance
    {
        get => _currentBalance;
        set => SetProperty(ref _currentBalance, value);
    }

    public decimal TotalDebit
    {
        get => _totalDebit;
        set => SetProperty(ref _totalDebit, value);
    }

    public decimal TotalCredit
    {
        get => _totalCredit;
        set => SetProperty(ref _totalCredit, value);
    }

    public ObservableCollection<TransactionRow> Transactions { get; } = new();

    public RelayCommand ApplyFilterCommand { get; }
    public RelayCommand ClearFilterCommand { get; }
    public RelayCommand AddGoldCommand { get; }
    public RelayCommand AddPaymentCommand { get; }
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
        AddGoldCommand = new RelayCommand(_ => AddTransaction(TransactionType.GoldGiven), _ => Supplier != null);
        AddPaymentCommand = new RelayCommand(_ => AddTransaction(TransactionType.PaymentIssued), _ => Supplier != null);
        EditTransactionCommand = new RelayCommand(_ => EditTransaction(), _ => Supplier != null && SelectedTransaction != null);
        DeleteTransactionCommand = new RelayCommand(_ => DeleteTransaction(), _ => Supplier != null && SelectedTransaction != null);
        PrintStatementCommand = new RelayCommand(_ => PrintStatement(), _ => Supplier != null);

        LoadSupplierOptions();
    }

    private void LoadSupplierOptions()
    {
        SupplierOptions.Clear();
        SupplierOptions.Add(new SupplierListItem
        {
            Id = 0,
            Name = System.Windows.Application.Current.TryFindResource("LblAllSuppliers")?.ToString() ?? "All suppliers"
        });

        foreach (var supplier in AppServices.SupplierService.GetSuppliers())
        {
            SupplierOptions.Add(new SupplierListItem { Id = supplier.Id, Name = supplier.Name });
        }

        SelectedSupplierOption ??= SupplierOptions.FirstOrDefault();
    }

    public void RefreshLocalization()
    {
        LoadSupplierOptions();
        if (Supplier != null)
        {
            SelectedSupplierOption = SupplierOptions.FirstOrDefault(s => s.Id == Supplier.Id);
        }
        else
        {
            SelectedSupplierOption = SupplierOptions.FirstOrDefault(s => s.Id == 0);
        }
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
        Supplier = null;
        LoadSupplierOptions();
        SelectedSupplierOption = SupplierOptions.FirstOrDefault(s => s.Id == 0);
        FromDate ??= DateTime.Today.AddMonths(-1);
        ToDate ??= DateTime.Today;
        Load();
    }

    private void Load()
    {
        Transactions.Clear();

        var suppliers = AppServices.SupplierService.GetSuppliers();
        var supplierLookup = suppliers.ToDictionary(s => s.Id, s => s.Name);

        var transactions = Supplier != null
            ? AppServices.TransactionService.GetTransactions(Supplier.Id, FromDate, ToDate)
            : AppServices.TransactionService.GetTransactions(FromDate, ToDate);

        var balanceBySupplier = new Dictionary<int, decimal>();
        decimal totalDebit = 0m;
        decimal totalCredit = 0m;

        foreach (var transaction in transactions)
        {
            var amount = transaction.Amount;
            balanceBySupplier.TryGetValue(transaction.SupplierId, out var bal);
            bal += IsIncrease(transaction.Type) ? amount : -amount;
            balanceBySupplier[transaction.SupplierId] = bal;
            totalDebit += IsIncrease(transaction.Type) ? amount : 0m;
            totalCredit += IsDecrease(transaction.Type) ? amount : 0m;

            Transactions.Add(new TransactionRow
            {
                Id = transaction.Id,
                SupplierName = supplierLookup.TryGetValue(transaction.SupplierId, out var sname) ? sname : string.Empty,
                Date = transaction.Date,
                Type = transaction.Type,
                Description = transaction.Description ?? string.Empty,
                Amount = amount,
                Balance = bal,
                Notes = transaction.Notes ?? string.Empty
            });
        }

        CurrentBalance = Supplier != null && balanceBySupplier.TryGetValue(Supplier.Id, out var b) ? b : 0m;
        TotalDebit = totalDebit;
        TotalCredit = totalCredit;
    }

    private void AddTransaction(TransactionType type)
    {
        if (Supplier == null)
        {
            return;
        }

        var suppliers = AppServices.SupplierService.GetSuppliers()
            .Select(s => new SupplierListItem { Id = s.Id, Name = s.Name, Phone = s.Phone ?? string.Empty })
            .ToList();
        var dialog = new Views.TransactionWindow(Supplier.Id, Supplier.Name, suppliers, null, DateTime.Today, type, string.Empty, 0m, string.Empty);
        if (dialog.ShowDialog() == true)
        {
            AppServices.TransactionService.AddTransaction(
                Supplier.Id,
                dialog.TransactionDate,
                dialog.TransactionType,
                dialog.Description,
                dialog.Amount,
                dialog.GoldWeight,
                dialog.GoldPurity,
                TransactionCategory.None,
                dialog.Notes);
            Load();
        }
    }

    private void EditTransaction()
    {
        if (Supplier == null || SelectedTransaction == null)
        {
            return;
        }

        var suppliers = AppServices.SupplierService.GetSuppliers()
            .Select(s => new SupplierListItem { Id = s.Id, Name = s.Name, Phone = s.Phone ?? string.Empty })
            .ToList();
        var dialog = new Views.TransactionWindow(
            Supplier.Id,
            Supplier.Name,
            suppliers,
            SelectedTransaction.Id,
            SelectedTransaction.Date,
            SelectedTransaction.Type,
            SelectedTransaction.Description,
            SelectedTransaction.Amount,
            SelectedTransaction.Notes);

        if (dialog.ShowDialog() == true)
        {
            AppServices.TransactionService.UpdateTransaction(
                SelectedTransaction.Id,
                Supplier.Id,
                dialog.TransactionDate,
                dialog.TransactionType,
                dialog.Description,
                dialog.Amount,
                dialog.GoldWeight,
                dialog.GoldPurity,
                TransactionCategory.None,
                dialog.Notes);
            Load();
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

    private static bool IsIncrease(TransactionType type)
    {
        return type == TransactionType.GoldGiven || type == TransactionType.PaymentReceived;
    }

    private static bool IsDecrease(TransactionType type)
    {
        return type == TransactionType.GoldReceived || type == TransactionType.PaymentIssued;
    }
}
