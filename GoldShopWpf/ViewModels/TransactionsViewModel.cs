using System.Collections.ObjectModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class TransactionsViewModel : ViewModelBase
{
    private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    private DateTime? _toDate = DateTime.Today;
    private string _searchText = string.Empty;
    private SupplierListItem? _selectedSupplier;
    private TransactionListItem? _selectedTransaction;

    public ObservableCollection<SupplierListItem> Suppliers { get; } = new();
    public ObservableCollection<TransactionListItem> Transactions { get; } = new();
    public ObservableCollection<TransactionListItem> FilteredTransactions { get; } = new();

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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public SupplierListItem? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                ApplyFilter();
            }
        }
    }

    public TransactionListItem? SelectedTransaction
    {
        get => _selectedTransaction;
        set
        {
            if (SetProperty(ref _selectedTransaction, value))
            {
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public TransactionsViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        AddCommand = new RelayCommand(_ => AddTransaction());
        EditCommand = new RelayCommand(_ => EditTransaction(), _ => SelectedTransaction != null);
        DeleteCommand = new RelayCommand(_ => DeleteTransaction(), _ => SelectedTransaction != null);

        Load();
    }

    public void Load()
    {
        Suppliers.Clear();
        Transactions.Clear();
        FilteredTransactions.Clear();

        var supplierService = AppServices.SupplierService;
        var suppliers = supplierService.GetSuppliers();
        var allLabel = System.Windows.Application.Current.TryFindResource("LblAllSuppliers")?.ToString() ?? "All suppliers";
        Suppliers.Add(new SupplierListItem { Id = 0, Name = allLabel, Phone = string.Empty });
        foreach (var supplier in suppliers)
        {
            Suppliers.Add(new SupplierListItem
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Phone = supplier.Phone ?? string.Empty
            });
        }

        var supplierLookup = suppliers.ToDictionary(s => s.Id, s => s.Name);
        var transactions = AppServices.TransactionService.GetTransactions(FromDate, ToDate);
        foreach (var transaction in transactions)
        {
            Transactions.Add(new TransactionListItem
            {
                Id = transaction.Id,
                SupplierId = transaction.SupplierId,
                SupplierName = supplierLookup.TryGetValue(transaction.SupplierId, out var name) ? name : "",
                Date = transaction.Date,
                Type = transaction.Type,
                Details = transaction.Description ?? string.Empty,
                Amount = transaction.Amount,
                Notes = transaction.Notes ?? string.Empty
            });
        }

        SelectedSupplier ??= Suppliers.FirstOrDefault();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredTransactions.Clear();
        var query = SearchText.Trim().ToLowerInvariant();

        foreach (var transaction in Transactions)
        {
            if (SelectedSupplier != null && SelectedSupplier.Id != 0 && transaction.SupplierId != SelectedSupplier.Id)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query) &&
                !transaction.SupplierName.ToLowerInvariant().Contains(query) &&
                !transaction.Details.ToLowerInvariant().Contains(query))
            {
                continue;
            }

            FilteredTransactions.Add(transaction);
        }
    }

    private void AddTransaction()
    {
        var dialog = new Views.TransactionWindow(null, "Select Supplier", Suppliers);
        if (dialog.ShowDialog() == true)
        {
            AppServices.TransactionService.AddTransaction(
                dialog.SupplierId,
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
        if (SelectedTransaction == null)
        {
            return;
        }

        var dialog = new Views.TransactionWindow(
            SelectedTransaction.SupplierId,
            SelectedTransaction.SupplierName,
            Suppliers,
            SelectedTransaction.Id,
            SelectedTransaction.Date,
            SelectedTransaction.Type,
            SelectedTransaction.Details,
            SelectedTransaction.Amount,
            SelectedTransaction.Notes);

        if (dialog.ShowDialog() == true)
        {
            AppServices.TransactionService.UpdateTransaction(
                SelectedTransaction.Id,
                dialog.SupplierId,
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
            "Delete this transaction?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            AppServices.TransactionService.DeleteTransaction(SelectedTransaction.Id);
            Load();
        }
    }
}
