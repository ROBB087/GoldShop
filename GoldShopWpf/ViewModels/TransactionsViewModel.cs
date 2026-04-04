using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class TransactionsViewModel : ViewModelBase
{
    private const int AllTradersId = 0;
    private const int PageSize = 200;
    private static readonly TimeSpan DefaultLookback = TimeSpan.FromDays(30);

    private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    private DateTime? _toDate = DateTime.Today;
    private string _searchText = string.Empty;
    private SupplierListItem? _selectedSupplier;
    private TransactionListItem? _selectedTransaction;
    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalRecords;
    private bool _isRefreshingSuppliers;

    public ObservableCollection<SupplierListItem> Suppliers { get; } = new();
    public ObservableCollection<TransactionListItem> FilteredTransactions { get; } = new();
    public bool HasTransactions => FilteredTransactions.Count > 0;
    public bool? AreAllVisibleSelected
    {
        get
        {
            if (FilteredTransactions.Count == 0)
            {
                return false;
            }

            var selectedCount = FilteredTransactions.Count(item => item.IsSelected);
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
    public int CheckedCount => FilteredTransactions.Count(item => item.IsSelected);
    public int EffectiveSelectedCount => CheckedCount > 0 ? CheckedCount : SelectedTransaction == null ? 0 : 1;
    public string SelectedCountLabel => UiText.Format("LblSelectedCount", EffectiveSelectedCount);
    public string RowsCountLabel => UiText.Format("LblRows", FilteredTransactions.Count);
    public bool HasCheckedTransactions => CheckedCount > 0;
    public bool HasSelection => CheckedCount > 0 || SelectedTransaction != null;

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
                Load();
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
                if (_isRefreshingSuppliers)
                {
                    return;
                }

                _currentPage = 1;
                Load();
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

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(PageSummary));
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(PageSummary));
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalRecords
    {
        get => _totalRecords;
        private set
        {
            if (SetProperty(ref _totalRecords, value))
            {
                OnPropertyChanged(nameof(PageSummary));
            }
        }
    }

    public string PageSummary => UiText.Format("LblPageSummary", CurrentPage, TotalPages, TotalRecords);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand AddTraderCommand { get; }
    public RelayCommand ResetFiltersCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand ViewRowCommand { get; }
    public RelayCommand EditRowCommand { get; }
    public RelayCommand DeleteRowCommand { get; }
    public RelayCommand PreviousPageCommand { get; }
    public RelayCommand NextPageCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }

    public TransactionsViewModel()
    {
        FilteredTransactions.CollectionChanged += OnTransactionsCollectionChanged;
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
        AddCommand = new RelayCommand(_ => AddTransaction());
        AddTraderCommand = new RelayCommand(_ => AddTrader());
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
        EditCommand = new RelayCommand(_ => EditTransaction(null), _ => GetEditableTransaction(null) != null);
        DeleteCommand = new RelayCommand(_ => DeleteTransactions(null), _ => GetDeleteTargets(null).Count > 0);
        ViewRowCommand = new RelayCommand(row => ViewTransaction(row as TransactionListItem), row => row is TransactionListItem);
        EditRowCommand = new RelayCommand(row => EditTransaction(row), row => GetEditableTransaction(row) != null);
        DeleteRowCommand = new RelayCommand(row => DeleteTransactions(row), row => GetDeleteTargets(row).Count > 0);
        PreviousPageCommand = new RelayCommand(_ => ChangePage(-1), _ => HasPreviousPage);
        NextPageCommand = new RelayCommand(_ => ChangePage(1), _ => HasNextPage);
        ClearSelectionCommand = new RelayCommand(_ => ClearSelection(), _ => HasSelection);
        LoadSuppliers();
        Load();
    }

    public void Load()
    {
        LoadSuppliers();
        _ = LoadAsync();
    }

    public void RefreshLocalization()
    {
        LoadSuppliers();
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            ToastService.ShowWarning(UiText.L("MsgFromBeforeTo"));
            return;
        }

        await RunBusyAsync(UiText.L("MsgLoadingTransactions"), async () =>
        {
            var supplierId = SelectedSupplier?.Id > 0 ? SelectedSupplier.Id : (int?)null;
            var requestedPage = CurrentPage;

            var pageData = await Task.Run(() =>
            {
                var suppliers = AppServices.SupplierService.GetSuppliers();
                var supplierLookup = suppliers.ToDictionary(s => s.Id, s => s.Name);
                var page = AppServices.TransactionService.GetTransactionsPage(supplierId, FromDate, ToDate, requestedPage, PageSize);
                return (supplierLookup, page);
            });

            var effectiveTotalPages = Math.Max(pageData.page.TotalPages, 1);
            var page = pageData.page;
            if (requestedPage > effectiveTotalPages)
            {
                CurrentPage = effectiveTotalPages;
                page = await Task.Run(() => AppServices.TransactionService.GetTransactionsPage(supplierId, FromDate, ToDate, CurrentPage, PageSize));
            }

            TotalRecords = page.TotalCount;
            TotalPages = Math.Max(page.TotalPages, 1);

            FilteredTransactions.Clear();
            var query = SearchText.Trim().ToLowerInvariant();

            foreach (var transaction in page.Items)
            {
                var item = new TransactionListItem
                {
                    Id = transaction.Id,
                    SupplierId = transaction.SupplierId,
                    SupplierName = pageData.supplierLookup.TryGetValue(transaction.SupplierId, out var name) ? name : string.Empty,
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
                };

                if (!string.IsNullOrWhiteSpace(query) &&
                    !item.SupplierName.ToLowerInvariant().Contains(query) &&
                    !item.ItemName.ToLowerInvariant().Contains(query) &&
                    !item.Traceability.ToLowerInvariant().Contains(query) &&
                    !item.Notes.ToLowerInvariant().Contains(query))
                {
                    continue;
                }

                FilteredTransactions.Add(item);
            }

            SelectedTransaction = null;
            OnPropertyChanged(nameof(HasTransactions));
            OnPropertyChanged(nameof(RowsCountLabel));
            RefreshSelectionState();
        }, UiText.L("MsgGenericError"));
    }

    public void SetVisibleSelection(bool isSelected)
    {
        foreach (var transaction in FilteredTransactions)
        {
            transaction.IsSelected = isSelected;
        }

        RefreshSelectionState();
    }

    private void LoadSuppliers()
    {
        _isRefreshingSuppliers = true;
        var selectedId = SelectedSupplier?.Id ?? AllTradersId;
        Suppliers.Clear();
        Suppliers.Add(new SupplierListItem { Id = AllTradersId, Name = UiText.L("LblAllSuppliers") });
        foreach (var supplier in AppServices.SupplierService.GetSuppliers())
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

        _selectedSupplier = Suppliers.FirstOrDefault(s => s.Id == selectedId) ?? Suppliers.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedSupplier));
        _isRefreshingSuppliers = false;
    }

    private async void AddTransaction()
    {
        var selectedSupplierId = SelectedSupplier?.Id > 0 ? SelectedSupplier.Id : (int?)null;
        var defaults = AppServices.PricingSettingsService.GetLatest();
        var dialog = new Views.TransactionWindow(
            selectedSupplierId,
            Suppliers.Where(s => s.Id != AllTradersId).ToList(),
            defaults.DefaultManufacturingPerGram,
            defaults.DefaultImprovementPerGram);

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var supplierId = dialog.SupplierId;
        var transactionDate = dialog.TransactionDate;
        var transactionCategory = dialog.TransactionCategory;
        var originalWeight = dialog.OriginalWeight;
        var itemName = dialog.ItemName;
        var originalKarat = dialog.OriginalKarat;
        var manufacturingPerGram = dialog.ManufacturingPerGram;
        var improvementPerGram = dialog.ImprovementPerGram;
        var notes = dialog.Notes;

        try
        {
            await RunBusyAsync(UiText.L("MsgSavingTransaction"), async () =>
            {
                await Task.Run(() => AppServices.TransactionService.AddTransaction(
                    supplierId,
                    transactionDate,
                    transactionCategory,
                    originalWeight,
                    itemName,
                    originalKarat,
                    manufacturingPerGram,
                    improvementPerGram,
                    notes));
            }, string.Empty, rethrow: true);

            ToastService.ShowSuccess(UiText.L("MsgTransactionSaved"));
            await LoadAsync();
        }
        catch (ArgumentException ex)
        {
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
        }
    }

    private void AddTrader()
    {
        var dialog = new Views.SupplierWindow();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        AppServices.SupplierService.AddSupplier(
            dialog.SupplierName,
            dialog.SupplierPhone,
            dialog.WorkerName,
            dialog.WorkerPhone,
            dialog.SupplierNotes);

        LoadSuppliers();
        ToastService.ShowSuccess(UiText.L("MsgSupplierSaved"));
    }

    private void ViewTransaction(TransactionListItem? transaction)
    {
        if (transaction == null)
        {
            return;
        }

        var dialog = new Views.TransactionWindow(transaction, Suppliers.Where(s => s.Id != AllTradersId).ToList(), isReadOnly: true);
        dialog.ShowDialog();
    }

    private async void EditTransaction(object? parameter)
    {
        var transaction = GetEditableTransaction(parameter);
        if (transaction == null)
        {
            return;
        }

        var dialog = new Views.TransactionWindow(transaction, Suppliers.Where(s => s.Id != AllTradersId).ToList());
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var supplierId = dialog.SupplierId;
        var transactionDate = dialog.TransactionDate;
        var transactionCategory = dialog.TransactionCategory;
        var originalWeight = dialog.OriginalWeight;
        var itemName = dialog.ItemName;
        var originalKarat = dialog.OriginalKarat;
        var manufacturingPerGram = dialog.ManufacturingPerGram;
        var improvementPerGram = dialog.ImprovementPerGram;
        var notes = dialog.Notes;

        try
        {
            await RunBusyAsync(UiText.L("MsgSavingTransaction"), async () =>
            {
                await Task.Run(() => AppServices.TransactionService.UpdateTransaction(
                    transaction.Id,
                    supplierId,
                    transactionDate,
                    transactionCategory,
                    originalWeight,
                    itemName,
                    originalKarat,
                    manufacturingPerGram,
                    improvementPerGram,
                    notes));
            }, string.Empty, rethrow: true);

            ToastService.ShowSuccess(UiText.L("MsgTransactionUpdated", UiText.L("MsgTransactionSaved")));
            await LoadAsync();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
        }
    }

    private async void DeleteTransactions(object? parameter)
    {
        var deleteTargets = GetDeleteTargets(parameter);
        if (deleteTargets.Count == 0)
        {
            return;
        }

        var message = deleteTargets.Count == 1
            ? UiText.L("MsgDeleteTransactionConfirmShort")
            : UiText.Format("MsgDeleteSelectedRecordsConfirm", deleteTargets.Count);

        var result = System.Windows.MessageBox.Show(
            $"{message}{Environment.NewLine}{Environment.NewLine}{UiText.L("MsgFinancialTotalsWarning")}",
            UiText.L("TitleConfirmDelete"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await RunBusyAsync(UiText.L("MsgDeletingTransaction", "Deleting transaction..."), async () =>
            {
                await Task.Run(() =>
                {
                    foreach (var target in deleteTargets)
                    {
                        AppServices.TransactionService.DeleteTransaction(target.Id);
                    }
                });
            }, string.Empty, rethrow: true);

            ToastService.ShowSuccess(deleteTargets.Count == 1
                ? UiText.L("MsgTransactionDeleted", "Transaction deleted successfully.")
                : UiText.Format("MsgTransactionsDeleted", deleteTargets.Count));
            await LoadAsync();
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
        }
    }

    private void ResetFilters()
    {
        FromDate = DateTime.Today.Subtract(DefaultLookback);
        ToDate = DateTime.Today;
        SearchText = string.Empty;
        SelectedSupplier = Suppliers.FirstOrDefault();
        CurrentPage = 1;
        _ = LoadAsync();
    }

    private void ChangePage(int delta)
    {
        CurrentPage = Math.Clamp(CurrentPage + delta, 1, TotalPages);
        Load();
    }

    private void ClearSelection()
    {
        foreach (var transaction in FilteredTransactions)
        {
            transaction.IsSelected = false;
        }

        SelectedTransaction = null;
        RefreshSelectionState();
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

    private TransactionListItem? GetEditableTransaction(object? parameter)
    {
        if (parameter is TransactionListItem transaction)
        {
            return transaction;
        }

        var checkedTransactions = FilteredTransactions.Where(item => item.IsSelected).ToList();
        if (checkedTransactions.Count == 1)
        {
            return checkedTransactions[0];
        }

        if (checkedTransactions.Count > 1)
        {
            return null;
        }

        return SelectedTransaction;
    }

    private List<TransactionListItem> GetDeleteTargets(object? parameter)
    {
        if (parameter is TransactionListItem transaction)
        {
            return [transaction];
        }

        var checkedTransactions = FilteredTransactions.Where(item => item.IsSelected).ToList();
        if (checkedTransactions.Count > 0)
        {
            return checkedTransactions;
        }

        return SelectedTransaction == null ? [] : [SelectedTransaction];
    }

    private void RefreshSelectionState()
    {
        OnPropertyChanged(nameof(AreAllVisibleSelected));
        OnPropertyChanged(nameof(CheckedCount));
        OnPropertyChanged(nameof(EffectiveSelectedCount));
        OnPropertyChanged(nameof(SelectedCountLabel));
        OnPropertyChanged(nameof(HasCheckedTransactions));
        OnPropertyChanged(nameof(HasSelection));
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        EditRowCommand.RaiseCanExecuteChanged();
        DeleteRowCommand.RaiseCanExecuteChanged();
        ClearSelectionCommand.RaiseCanExecuteChanged();
    }

}
