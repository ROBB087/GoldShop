using System.Collections.ObjectModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class TransactionsViewModel : ViewModelBase
{
    private const int AllTradersId = 0;
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
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            System.Windows.MessageBox.Show(UiText.L("MsgFromBeforeTo"), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var selectedSupplierId = SelectedSupplier?.Id;
        Suppliers.Clear();
        Transactions.Clear();
        FilteredTransactions.Clear();

        var supplierService = AppServices.SupplierService;
        var suppliers = supplierService.GetSuppliers();
        Suppliers.Add(new SupplierListItem
        {
            Id = AllTradersId,
            Name = UiText.L("LblAllSuppliers")
        });

        foreach (var supplier in suppliers)
        {
            Suppliers.Add(new SupplierListItem
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Phone = supplier.Phone ?? string.Empty,
                WorkerName = supplier.WorkerName ?? string.Empty,
                WorkerPhone = supplier.WorkerPhone ?? string.Empty
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
                SupplierName = supplierLookup.TryGetValue(transaction.SupplierId, out var name) ? name : string.Empty,
                Date = transaction.Date,
                Type = transaction.Type,
                Category = transaction.Category,
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

        SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == selectedSupplierId)
            ?? Suppliers.FirstOrDefault();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredTransactions.Clear();
        var query = SearchText.Trim().ToLowerInvariant();

        foreach (var transaction in Transactions)
        {
            if (SelectedSupplier != null &&
                SelectedSupplier.Id != AllTradersId &&
                transaction.SupplierId != SelectedSupplier.Id)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query) &&
                !transaction.SupplierName.ToLowerInvariant().Contains(query) &&
                !transaction.Traceability.ToLowerInvariant().Contains(query) &&
                !transaction.Notes.ToLowerInvariant().Contains(query))
            {
                continue;
            }

            FilteredTransactions.Add(transaction);
        }
    }

    private void AddTransaction()
    {
        var selectedSupplierId = SelectedSupplier?.Id > 0 ? SelectedSupplier.Id : (int?)null;
        var dialog = new Views.TransactionWindow(selectedSupplierId, GetPopupSuppliers());
        if (dialog.ShowDialog() == true)
        {
            SaveTransaction(dialog, null);
        }
    }

    private void EditTransaction()
    {
        if (SelectedTransaction == null)
        {
            return;
        }

        var dialog = new Views.TransactionWindow(SelectedTransaction, GetPopupSuppliers());
        if (dialog.ShowDialog() == true)
        {
            SaveTransaction(dialog, SelectedTransaction.Id);
        }
    }

    private List<SupplierListItem> GetPopupSuppliers()
        => Suppliers.Where(s => s.Id != AllTradersId).ToList();

    private void SaveTransaction(Views.TransactionWindow dialog, int? transactionId)
    {
        try
        {
            if (transactionId.HasValue)
            {
                AppServices.TransactionService.UpdateTransaction(
                    transactionId.Value,
                    dialog.SupplierId,
                    dialog.TransactionDate,
                    dialog.TransactionCategory,
                    dialog.OriginalWeight,
                    dialog.OriginalKarat,
                    dialog.ManufacturingPerGram,
                    dialog.ImprovementPerGram,
                    dialog.Notes);
            }
            else
            {
                AppServices.TransactionService.AddTransaction(
                    dialog.SupplierId,
                    dialog.TransactionDate,
                    dialog.TransactionCategory,
                    dialog.OriginalWeight,
                    dialog.OriginalKarat,
                    dialog.ManufacturingPerGram,
                    dialog.ImprovementPerGram,
                    dialog.Notes);
            }

            Load();
        }
        catch (ArgumentException ex)
        {
            System.Windows.MessageBox.Show(UiText.LocalizeException(ex.Message), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private void DeleteTransaction()
    {
        if (SelectedTransaction == null)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            UiText.L("MsgDeleteTransactionConfirmShort"),
            UiText.L("TitleConfirmDelete"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            AppServices.TransactionService.DeleteTransaction(SelectedTransaction.Id);
            Load();
        }
    }
}
