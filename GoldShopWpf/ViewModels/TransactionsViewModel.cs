using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class TransactionsViewModel : ViewModelBase
{
    private const int AllTradersId = 0;
    private static readonly TimeSpan DefaultLookback = TimeSpan.FromDays(30);
    private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    private DateTime? _toDate = DateTime.Today;
    private string _searchText = string.Empty;
    private SupplierListItem? _selectedSupplier;
    private TransactionListItem? _selectedTransaction;

    public ObservableCollection<SupplierListItem> Suppliers { get; } = new();
    public ObservableCollection<TransactionListItem> Transactions { get; } = new();
    public ObservableCollection<TransactionListItem> FilteredTransactions { get; } = new();
    public bool? AreAllVisibleSelected
    {
        get
        {
            if (FilteredTransactions.Count == 0)
            {
                return false;
            }

            var selectedCount = FilteredTransactions.Count(transaction => transaction.IsSelected);
            if (selectedCount == 0)
            {
                return false;
            }

            return selectedCount == FilteredTransactions.Count ? true : null;
        }
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            foreach (var transaction in FilteredTransactions)
            {
                transaction.IsSelected = value.Value;
            }

            RefreshSelectionState();
        }
    }

    public int CheckedCount => FilteredTransactions.Count(transaction => transaction.IsSelected);
    public string SelectedCountLabel => UiText.Format("LblSelectedCount", CheckedCount);
    public bool HasCheckedTransactions => CheckedCount > 0;

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
                RefreshSelectionState();
            }
        }
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand ResetFiltersCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand ViewRowCommand { get; }
    public RelayCommand EditRowCommand { get; }
    public RelayCommand DeleteRowCommand { get; }

    public TransactionsViewModel()
    {
        Transactions.CollectionChanged += OnTransactionsCollectionChanged;
        RefreshCommand = new RelayCommand(_ => Load());
        AddCommand = new RelayCommand(_ => AddTransaction());
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
        EditCommand = new RelayCommand(_ => EditTransaction(), _ => GetBulkEditableTransaction() != null);
        DeleteCommand = new RelayCommand(_ => DeleteTransactions(), _ => GetCheckedTransactions().Count > 0);
        ViewRowCommand = new RelayCommand(row => ViewTransaction(row as TransactionListItem), row => row is TransactionListItem);
        EditRowCommand = new RelayCommand(row => EditTransaction(row as TransactionListItem), row => row is TransactionListItem);
        DeleteRowCommand = new RelayCommand(row => DeleteTransactions(row as TransactionListItem), row => row is TransactionListItem);

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
        SelectedTransaction = null;

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
                ItemName = transaction.ItemName ?? string.Empty,
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
        foreach (var transaction in Transactions.Where(transaction => transaction.IsSelected))
        {
            transaction.IsSelected = false;
        }

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
                !transaction.ItemName.ToLowerInvariant().Contains(query) &&
                !transaction.Traceability.ToLowerInvariant().Contains(query) &&
                !transaction.Notes.ToLowerInvariant().Contains(query))
            {
                continue;
            }

            FilteredTransactions.Add(transaction);
        }

        RefreshSelectionState();
    }

    private void AddTransaction()
    {
        var selectedSupplierId = SelectedSupplier?.Id > 0 ? SelectedSupplier.Id : (int?)null;
        var defaults = AppServices.PricingSettingsService.GetLatest();
        var dialog = new Views.TransactionWindow(
            selectedSupplierId,
            GetPopupSuppliers(),
            defaults.DefaultManufacturingPerGram,
            defaults.DefaultImprovementPerGram);
        if (dialog.ShowDialog() == true)
        {
            SaveTransaction(dialog, null);
        }
    }

    private void ViewTransaction(TransactionListItem? transaction)
    {
        if (transaction == null)
        {
            return;
        }

        var dialog = new Views.TransactionWindow(transaction, GetPopupSuppliers(), isReadOnly: true);
        dialog.ShowDialog();
    }

    private void EditTransaction(TransactionListItem? transaction = null)
    {
        var editableTransaction = transaction ?? GetBulkEditableTransaction();
        if (editableTransaction == null)
        {
            return;
        }

        var dialog = new Views.TransactionWindow(editableTransaction, GetPopupSuppliers());
        if (dialog.ShowDialog() == true)
        {
            SaveTransaction(dialog, editableTransaction.Id);
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
                    dialog.ItemName,
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
                    dialog.ItemName,
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

    private void DeleteTransactions(TransactionListItem? specificTransaction = null)
    {
        var deleteTargets = specificTransaction == null ? GetCheckedTransactions() : [specificTransaction];
        if (deleteTargets.Count == 0)
        {
            return;
        }

        var message = GetDeleteConfirmationMessage(deleteTargets.Count, FilteredTransactions.Count);
        var result = System.Windows.MessageBox.Show(
            message,
            UiText.L("TitleConfirmDelete"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            foreach (var transaction in deleteTargets)
            {
                AppServices.TransactionService.DeleteTransaction(transaction.Id);
            }

            Load();
        }
    }

    private void ResetFilters()
    {
        FromDate = DateTime.Today.Subtract(DefaultLookback);
        ToDate = DateTime.Today;
        SearchText = string.Empty;
        SelectedSupplier = Suppliers.FirstOrDefault();
        ApplyFilter();
    }

    private void OnTransactionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<TransactionListItem>())
            {
                item.PropertyChanged -= OnTransactionPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<TransactionListItem>())
            {
                item.PropertyChanged += OnTransactionPropertyChanged;
            }
        }
    }

    private void OnTransactionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransactionListItem.IsSelected))
        {
            RefreshSelectionState();
        }
    }

    private List<TransactionListItem> GetCheckedTransactions()
    {
        return FilteredTransactions.Where(transaction => transaction.IsSelected).ToList();
    }

    private TransactionListItem? GetBulkEditableTransaction()
    {
        var checkedTransactions = GetCheckedTransactions();
        if (checkedTransactions.Count == 1)
        {
            return checkedTransactions[0];
        }

        return null;
    }

    private void RefreshSelectionState()
    {
        OnPropertyChanged(nameof(AreAllVisibleSelected));
        OnPropertyChanged(nameof(CheckedCount));
        OnPropertyChanged(nameof(SelectedCountLabel));
        OnPropertyChanged(nameof(HasCheckedTransactions));
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
    }

    private static string GetDeleteConfirmationMessage(int targetCount, int visibleCount)
    {
        if (targetCount > 0 && visibleCount > 0 && targetCount == visibleCount)
        {
            return UiText.L("MsgDeleteAllRecordsConfirm");
        }

        return targetCount == 1
            ? UiText.L("MsgDeleteSingleRecordConfirm")
            : UiText.Format("MsgDeleteSelectedRecordsConfirm", targetCount);
    }
}
