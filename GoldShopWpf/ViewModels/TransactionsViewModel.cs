using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class TransactionsViewModel : ViewModelBase
{
    private const int AllTradersId = 0;
    private const int PageSize = 50;
    private DateTime? _fromDate = DateTime.Today;
    private DateTime? _toDate = DateTime.Today;
    private string _searchText = string.Empty;
    private SupplierListItem? _selectedSupplier;
    private TransactionListItem? _selectedTransaction;
    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalRecords;
    private bool _isRefreshingSuppliers;
    private bool _suppressDateAutoRefresh;
    private CancellationTokenSource? _dateRefreshCts;
    private Dictionary<int, string> _supplierLookup = [];

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
        set
        {
            var normalized = value?.Date;
            if (SetProperty(ref _fromDate, normalized))
            {
                OnDateFilterChanged();
            }
        }
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            var normalized = value?.Date;
            if (SetProperty(ref _toDate, normalized))
            {
                OnDateFilterChanged();
            }
        }
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

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand AddCommand { get; }
    public RelayCommand AddTraderCommand { get; }
    public AsyncRelayCommand ResetFiltersCommand { get; }
    public AsyncRelayCommand EditCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public RelayCommand ViewRowCommand { get; }
    public AsyncRelayCommand EditRowCommand { get; }
    public AsyncRelayCommand DeleteRowCommand { get; }
    public AsyncRelayCommand PreviousPageCommand { get; }
    public AsyncRelayCommand NextPageCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }

    public TransactionsViewModel()
    {
        FilteredTransactions.CollectionChanged += OnTransactionsCollectionChanged;
        SupplierChangeNotifier.SuppliersChanged += OnSuppliersChanged;
        RefreshCommand = TrackCommand(new AsyncRelayCommand(_ => LoadAsync(), _ => !IsBusy));
        AddCommand = TrackCommand(new AsyncRelayCommand(_ => AddTransactionAsync(), _ => !IsBusy));
        AddTraderCommand = new RelayCommand(_ => AddTrader());
        ResetFiltersCommand = TrackCommand(new AsyncRelayCommand(_ => ResetFiltersAsync(), _ => !IsBusy));
        EditCommand = TrackCommand(new AsyncRelayCommand(_ => EditTransactionAsync(null), _ => !IsBusy && GetEditableTransaction(null) != null));
        DeleteCommand = TrackCommand(new AsyncRelayCommand(_ => DeleteTransactionsAsync(null), _ => !IsBusy && GetDeleteTargets(null).Count > 0));
        ViewRowCommand = new RelayCommand(row => ViewTransaction(row as TransactionListItem), row => row is TransactionListItem);
        EditRowCommand = TrackCommand(new AsyncRelayCommand(row => EditTransactionAsync(row), row => !IsBusy && GetEditableTransaction(row) != null));
        DeleteRowCommand = TrackCommand(new AsyncRelayCommand(row => DeleteTransactionsAsync(row), row => !IsBusy && GetDeleteTargets(row).Count > 0));
        PreviousPageCommand = TrackCommand(new AsyncRelayCommand(_ => ChangePageAsync(-1), _ => !IsBusy && HasPreviousPage));
        NextPageCommand = TrackCommand(new AsyncRelayCommand(_ => ChangePageAsync(1), _ => !IsBusy && HasNextPage));
        ClearSelectionCommand = new RelayCommand(_ => ClearSelection(), _ => HasSelection);
        LoadSuppliers();
        Load();
    }

    public void Load()
    {
        ObserveBackgroundTask(LoadAsync(), "TransactionsViewModel.Load");
    }

    public void RefreshLocalization()
    {
        LoadSuppliers();
        ObserveBackgroundTask(LoadAsync(), "TransactionsViewModel.RefreshLocalization");
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
            var from = FromDate?.Date;
            var to = ToDate?.Date;
            var supplierId = SelectedSupplier?.Id > 0 ? SelectedSupplier.Id : (int?)null;
            var requestedPage = CurrentPage;

            FilteredTransactions.Clear();
            var query = SearchText.Trim().ToLowerInvariant();
            var rows = await Task.Run(() =>
            {
                var transactions = supplierId.HasValue
                    ? AppServices.TransactionService.GetTransactions(supplierId.Value, from, to)
                    : AppServices.TransactionService.GetTransactions(from, to);
                var adjustments = supplierId.HasValue
                    ? AppServices.OpeningBalanceAdjustmentService.GetAdjustments(supplierId.Value, from, to)
                    : AppServices.OpeningBalanceAdjustmentService.GetAdjustments(from, to);

                return transactions
                    .Select(CreateTransactionListItem)
                    .Concat(adjustments.Select(CreateAdjustmentListItem))
                    .Where(item => string.IsNullOrWhiteSpace(query) ||
                        item.SupplierName.ToLowerInvariant().Contains(query) ||
                        item.ItemName.ToLowerInvariant().Contains(query) ||
                        item.Traceability.ToLowerInvariant().Contains(query) ||
                        item.Notes.ToLowerInvariant().Contains(query))
                    .OrderByDescending(item => item.Date)
                    .ThenByDescending(item => item.UpdatedAt)
                    .ThenByDescending(item => item.Id)
                    .ToList();
            });

            var totalRecords = rows.Count;
            var totalPages = Math.Max((int)Math.Ceiling((double)totalRecords / PageSize), 1);
            if (requestedPage > totalPages)
            {
                CurrentPage = totalPages;
            }

            TotalRecords = totalRecords;
            TotalPages = totalPages;

            foreach (var item in rows.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            {
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
        var supplierRecords = AppServices.SupplierService.GetSuppliers();
        _supplierLookup = supplierRecords.ToDictionary(s => s.Id, s => s.Name);
        Suppliers.Clear();
        Suppliers.Add(new SupplierListItem { Id = AllTradersId, Name = UiText.L("LblAllSuppliers") });
        foreach (var supplier in supplierRecords)
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

    private async Task AddTransactionAsync()
    {
        var selectedSupplierId = SelectedSupplier?.Id > 0 ? SelectedSupplier.Id : (int?)null;
        var defaults = AppServices.PricingSettingsService.GetLatest();
        var dialog = new Views.TransactionWindow(
            selectedSupplierId,
            Suppliers.Where(s => s.Id != AllTradersId).ToList(),
            defaults.DefaultManufacturingPerGram,
            defaults.DefaultManufacturingPerGram24,
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
                    notes,
                    Guid.NewGuid().ToString("N")));
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

        SupplierChangeNotifier.NotifySuppliersChanged();
        LoadSuppliers();
        ToastService.ShowSuccess(UiText.L("MsgSupplierSaved"));
    }

    private void OnSuppliersChanged()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            LoadSuppliers();
            ObserveBackgroundTask(LoadAsync(), "TransactionsViewModel.OnSuppliersChanged");
        });
    }

    private void ViewTransaction(TransactionListItem? transaction)
    {
        if (transaction == null)
        {
            return;
        }

        if (transaction.IsOpeningBalanceAdjustment)
        {
            var notes = string.IsNullOrWhiteSpace(transaction.Notes) ? "-" : transaction.Notes;
            System.Windows.MessageBox.Show(
                $"{UiText.L("LblDate")}: {transaction.Date:yyyy/MM/dd}{Environment.NewLine}{UiText.L("LblType")}: {transaction.TypeLabel}{Environment.NewLine}{UiText.L("LblAmount")}: {(transaction.TotalManufacturing + transaction.TotalImprovement):0.####}{Environment.NewLine}{UiText.L("LblNotes")}: {notes}",
                transaction.TypeLabel,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var dialog = new Views.TransactionWindow(transaction, Suppliers.Where(s => s.Id != AllTradersId).ToList(), isReadOnly: true);
        dialog.ShowDialog();
    }

    private async Task EditTransactionAsync(object? parameter)
    {
        var transaction = GetEditableTransaction(parameter);
        if (transaction == null)
        {
            return;
        }

        if (transaction.IsOpeningBalanceAdjustment)
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

    private async Task DeleteTransactionsAsync(object? parameter)
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
                        if (target.IsOpeningBalanceAdjustment)
                        {
                            AppServices.OpeningBalanceAdjustmentService.DeleteAdjustment(target.Id);
                        }
                        else
                        {
                            AppServices.TransactionService.DeleteTransaction(target.Id);
                        }
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

    private Task ResetFiltersAsync()
    {
        _suppressDateAutoRefresh = true;
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
        _suppressDateAutoRefresh = false;
        SearchText = string.Empty;
        SelectedSupplier = Suppliers.FirstOrDefault();
        CurrentPage = 1;
        return LoadAsync();
    }

    private void OnDateFilterChanged()
    {
        if (_suppressDateAutoRefresh)
        {
            return;
        }

        CurrentPage = 1;
        ScheduleDateAutoRefresh();
    }

    private void ScheduleDateAutoRefresh()
    {
        _dateRefreshCts?.Cancel();
        _dateRefreshCts?.Dispose();
        _dateRefreshCts = new CancellationTokenSource();
        var token = _dateRefreshCts.Token;

        ObserveBackgroundTask(AutoRefreshAsync(token), "TransactionsViewModel.DateFilterAutoRefresh");
    }

    private async Task AutoRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await LoadAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private Task ChangePageAsync(int delta)
    {
        CurrentPage = Math.Clamp(CurrentPage + delta, 1, TotalPages);
        return LoadAsync();
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

    private TransactionListItem CreateTransactionListItem(SupplierTransaction transaction)
    {
        return new TransactionListItem
        {
            Id = transaction.Id,
            SupplierId = transaction.SupplierId,
            SupplierName = _supplierLookup.TryGetValue(transaction.SupplierId, out var name) ? name : string.Empty,
            Date = transaction.Date,
            Type = transaction.Type,
            Category = transaction.Category,
            TypeLabel = FormatTransactionType(transaction.Category, transaction.Type),
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
    }

    private TransactionListItem CreateAdjustmentListItem(OpeningBalanceAdjustment adjustment)
    {
        var isManufacturing = adjustment.Type == OpeningBalanceAdjustmentType.Manufacturing;
        return new TransactionListItem
        {
            Id = adjustment.Id,
            SupplierId = adjustment.SupplierId,
            SupplierName = _supplierLookup.TryGetValue(adjustment.SupplierId, out var name) ? name : string.Empty,
            Date = adjustment.AdjustmentDate,
            IsOpeningBalanceAdjustment = true,
            AdjustmentType = adjustment.Type,
            Type = TransactionType.In,
            Category = isManufacturing ? "OpeningBalanceManufacturingAdjustment" : "OpeningBalanceImprovementAdjustment",
            TypeLabel = isManufacturing
                ? UiText.L("LblOpeningBalanceManufacturingAdjustment")
                : UiText.L("LblOpeningBalanceImprovementAdjustment"),
            ItemName = isManufacturing
                ? UiText.L("LblOpeningBalanceManufacturingAdjustment")
                : UiText.L("LblOpeningBalanceImprovementAdjustment"),
            TotalManufacturing = isManufacturing ? adjustment.Amount : 0m,
            TotalImprovement = isManufacturing ? 0m : adjustment.Amount,
            Traceability = UiText.L("LblOpeningBalanceAdjustmentEntry"),
            Notes = adjustment.Notes ?? string.Empty,
            CreatedAt = adjustment.CreatedAt,
            UpdatedAt = adjustment.UpdatedAt
        };
    }

    private static string FormatTransactionType(string category, TransactionType type)
    {
        return category switch
        {
            TransactionCategories.GoldOutbound => UiText.L("LblGoldOutboundReport"),
            TransactionCategories.GoldReceipt => UiText.L("LblGoldReceiptReport"),
            TransactionCategories.FinishedGoldReceipt => UiText.L("LblFinishedGoldReceiptReport"),
            TransactionCategories.CashPayment => UiText.L("LblCashPaymentReport"),
            _ => type.ToString()
        };
    }

}
