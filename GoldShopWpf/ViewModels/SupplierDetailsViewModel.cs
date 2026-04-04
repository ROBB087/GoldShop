using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GoldShopCore.Models;
using GoldShopCore.Services;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class SupplierDetailsViewModel : ViewModelBase
{
    private const int AllTradersId = 0;
    private const int DefaultPageSize = 50;

    private SupplierListItem? _supplier;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private SupplierListItem? _selectedTrader;
    private TransactionRow? _selectedTransaction;
    private decimal _totalGold21;
    private decimal _totalManufacturing;
    private decimal _totalImprovement;
    private decimal _manufacturingDiscounts;
    private decimal _improvementDiscounts;
    private int _transactionsCurrentPage = 1;
    private int _transactionsTotalPages = 1;
    private int _transactionsTotalCount;
    private int _discountsCurrentPage = 1;
    private int _discountsTotalPages = 1;
    private int _discountsTotalCount;
    private bool _isUpdatingSelection;
    private DiscountListItem? _selectedDiscount;

    public ObservableCollection<SupplierListItem> SupplierOptions { get; } = new();
    public ObservableCollection<TransactionRow> Transactions { get; } = new();
    public ObservableCollection<DiscountListItem> Discounts { get; } = new();

    public int PageSize { get; set; } = DefaultPageSize;

    public bool HasTransactions => Transactions.Count > 0;
    public bool HasDiscounts => Discounts.Count > 0;
    public bool? AreAllVisibleTransactionsSelected
    {
        get
        {
            if (Transactions.Count == 0)
            {
                return false;
            }

            var selectedCount = Transactions.Count(item => item.IsSelected);
            if (selectedCount == 0)
            {
                return false;
            }

            return selectedCount == Transactions.Count ? true : null;
        }
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            foreach (var transaction in Transactions)
            {
                transaction.IsSelected = value.Value;
            }

            RefreshTransactionSelectionState();
        }
    }
    public bool? AreAllVisibleDiscountsSelected
    {
        get
        {
            if (Discounts.Count == 0)
            {
                return false;
            }

            var selectedCount = Discounts.Count(item => item.IsSelected);
            if (selectedCount == 0)
            {
                return false;
            }

            return selectedCount == Discounts.Count ? true : null;
        }
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            foreach (var discount in Discounts)
            {
                discount.IsSelected = value.Value;
            }

            RefreshDiscountSelectionState();
        }
    }
    public int CheckedTransactionsCount => Transactions.Count(item => item.IsSelected);
    public int EffectiveTransactionsSelectedCount => CheckedTransactionsCount > 0 ? CheckedTransactionsCount : SelectedTransaction == null ? 0 : 1;
    public int CheckedDiscountsCount => Discounts.Count(item => item.IsSelected);
    public int EffectiveDiscountsSelectedCount => CheckedDiscountsCount > 0 ? CheckedDiscountsCount : SelectedDiscount == null ? 0 : 1;
    public bool HasTransactionSelection => CheckedTransactionsCount > 0 || SelectedTransaction != null;
    public bool HasDiscountSelection => CheckedDiscountsCount > 0 || SelectedDiscount != null;
    public string TransactionsSelectedCountLabel => UiText.Format("LblSelectedCount", EffectiveTransactionsSelectedCount);
    public string DiscountsSelectedCountLabel => UiText.Format("LblSelectedCount", EffectiveDiscountsSelectedCount);
    public string TransactionsRowsCountLabel => UiText.Format("LblRows", Transactions.Count);
    public string DiscountsRowsCountLabel => UiText.Format("LblRows", Discounts.Count);

    public SupplierListItem? SelectedTrader
    {
        get => _selectedTrader;
        set
        {
            if (SetProperty(ref _selectedTrader, value) && !_isUpdatingSelection)
            {
                ResetPages();
                if (value == null)
                {
                    Supplier = null;
                    ResetViewState();
                    return;
                }

                LoadTraderData(value.Id);
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
                PrintStatementCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(SupplierDisplayName));
            }
        }
    }

    public string SupplierDisplayName => Supplier?.Name ?? UiText.L("LblAllSuppliers");

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

    public DiscountListItem? SelectedDiscount
    {
        get => _selectedDiscount;
        set
        {
            if (SetProperty(ref _selectedDiscount, value))
            {
                EditDiscountCommand.RaiseCanExecuteChanged();
                DeleteDiscountCommand.RaiseCanExecuteChanged();
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
                OnPropertyChanged(nameof(TotalDiscounts));
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
                OnPropertyChanged(nameof(TotalDiscounts));
            }
        }
    }

    public decimal FinalManufacturing => TotalManufacturing - ManufacturingDiscounts;
    public decimal FinalImprovement => TotalImprovement - ImprovementDiscounts;
    public decimal TotalDiscounts => ManufacturingDiscounts + ImprovementDiscounts;

    public int TransactionsCurrentPage
    {
        get => _transactionsCurrentPage;
        set
        {
            var normalized = Math.Max(1, value);
            if (SetProperty(ref _transactionsCurrentPage, normalized))
            {
                OnPropertyChanged(nameof(TransactionsPageSummary));
                PreviousTransactionsPageCommand.RaiseCanExecuteChanged();
                NextTransactionsPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TransactionsTotalPages
    {
        get => _transactionsTotalPages;
        private set
        {
            if (SetProperty(ref _transactionsTotalPages, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(TransactionsPageSummary));
                PreviousTransactionsPageCommand.RaiseCanExecuteChanged();
                NextTransactionsPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TransactionsTotalCount
    {
        get => _transactionsTotalCount;
        private set
        {
            if (SetProperty(ref _transactionsTotalCount, value))
            {
                OnPropertyChanged(nameof(TransactionsPageSummary));
            }
        }
    }

    public int DiscountsCurrentPage
    {
        get => _discountsCurrentPage;
        set
        {
            var normalized = Math.Max(1, value);
            if (SetProperty(ref _discountsCurrentPage, normalized))
            {
                OnPropertyChanged(nameof(DiscountsPageSummary));
                PreviousDiscountsPageCommand.RaiseCanExecuteChanged();
                NextDiscountsPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int DiscountsTotalPages
    {
        get => _discountsTotalPages;
        private set
        {
            if (SetProperty(ref _discountsTotalPages, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(DiscountsPageSummary));
                PreviousDiscountsPageCommand.RaiseCanExecuteChanged();
                NextDiscountsPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int DiscountsTotalCount
    {
        get => _discountsTotalCount;
        private set
        {
            if (SetProperty(ref _discountsTotalCount, value))
            {
                OnPropertyChanged(nameof(DiscountsPageSummary));
            }
        }
    }

    public string TransactionsPageSummary => UiText.Format("LblPageSummary", TransactionsCurrentPage, TransactionsTotalPages, TransactionsTotalCount);
    public string DiscountsPageSummary => UiText.Format("LblPageSummary", DiscountsCurrentPage, DiscountsTotalPages, DiscountsTotalCount);
    public bool HasPreviousTransactionsPage => TransactionsCurrentPage > 1;
    public bool HasNextTransactionsPage => TransactionsCurrentPage < TransactionsTotalPages;
    public bool HasPreviousDiscountsPage => DiscountsCurrentPage > 1;
    public bool HasNextDiscountsPage => DiscountsCurrentPage < DiscountsTotalPages;

    public RelayCommand ApplyFilterCommand { get; }
    public RelayCommand ClearFilterCommand { get; }
    public RelayCommand AddTransactionCommand { get; }
    public RelayCommand AddDiscountCommand { get; }
    public RelayCommand EditTransactionCommand { get; }
    public RelayCommand DeleteTransactionCommand { get; }
    public RelayCommand EditDiscountCommand { get; }
    public RelayCommand DeleteDiscountCommand { get; }
    public RelayCommand EditTransactionRowCommand { get; }
    public RelayCommand DeleteTransactionRowCommand { get; }
    public RelayCommand EditDiscountRowCommand { get; }
    public RelayCommand DeleteDiscountRowCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand PrintStatementCommand { get; }
    public RelayCommand ViewTransactionRowCommand { get; }
    public RelayCommand ViewDiscountRowCommand { get; }
    public RelayCommand PreviousTransactionsPageCommand { get; }
    public RelayCommand NextTransactionsPageCommand { get; }
    public RelayCommand GoToTransactionsPageCommand { get; }
    public RelayCommand PreviousDiscountsPageCommand { get; }
    public RelayCommand NextDiscountsPageCommand { get; }
    public RelayCommand GoToDiscountsPageCommand { get; }
    public RelayCommand ClearTransactionSelectionCommand { get; }
    public RelayCommand ClearDiscountSelectionCommand { get; }

    public SupplierDetailsViewModel()
    {
        Transactions.CollectionChanged += OnTransactionsCollectionChanged;
        Discounts.CollectionChanged += OnDiscountsCollectionChanged;
        ApplyFilterCommand = new RelayCommand(_ =>
        {
            ResetPages();
            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        });
        ClearFilterCommand = new RelayCommand(_ =>
        {
            FromDate = null;
            ToDate = null;
            ResetPages();
            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        });
        AddTransactionCommand = new RelayCommand(_ => AddTransaction(), _ => Supplier != null);
        AddDiscountCommand = new RelayCommand(_ => AddDiscount(), _ => Supplier != null);
        EditTransactionCommand = new RelayCommand(_ => EditTransaction(null), _ => GetEditableTransaction(null) != null);
        DeleteTransactionCommand = new RelayCommand(_ => DeleteTransactions(null), _ => GetTransactionDeleteTargets(null).Count > 0);
        EditDiscountCommand = new RelayCommand(_ => EditDiscount(null), _ => GetEditableDiscount(null) != null);
        DeleteDiscountCommand = new RelayCommand(_ => DeleteDiscounts(null), _ => GetDiscountDeleteTargets(null).Count > 0);
        EditTransactionRowCommand = new RelayCommand(row => EditTransaction(row), row => GetEditableTransaction(row) != null);
        DeleteTransactionRowCommand = new RelayCommand(row => DeleteTransactions(row), row => GetTransactionDeleteTargets(row).Count > 0);
        EditDiscountRowCommand = new RelayCommand(row => EditDiscount(row), row => GetEditableDiscount(row) != null);
        DeleteDiscountRowCommand = new RelayCommand(row => DeleteDiscounts(row), row => GetDiscountDeleteTargets(row).Count > 0);
        RefreshCommand = new RelayCommand(_ =>
        {
            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        }, _ => SelectedTrader != null);
        PrintStatementCommand = new RelayCommand(_ => PrintStatement(), _ => Supplier != null);
        ViewTransactionRowCommand = new RelayCommand(row => ViewTransactionRow(row as TransactionRow), row => row is TransactionRow);
        ViewDiscountRowCommand = new RelayCommand(row => ViewDiscountRow(row as DiscountListItem), row => row is DiscountListItem);
        PreviousTransactionsPageCommand = new RelayCommand(_ => ChangeTransactionsPage(-1), _ => HasPreviousTransactionsPage);
        NextTransactionsPageCommand = new RelayCommand(_ => ChangeTransactionsPage(1), _ => HasNextTransactionsPage);
        GoToTransactionsPageCommand = new RelayCommand(_ => GoToTransactionsPage());
        PreviousDiscountsPageCommand = new RelayCommand(_ => ChangeDiscountsPage(-1), _ => HasPreviousDiscountsPage);
        NextDiscountsPageCommand = new RelayCommand(_ => ChangeDiscountsPage(1), _ => HasNextDiscountsPage);
        GoToDiscountsPageCommand = new RelayCommand(_ => GoToDiscountsPage());
        ClearTransactionSelectionCommand = new RelayCommand(_ => ClearTransactionSelection(), _ => HasTransactionSelection);
        ClearDiscountSelectionCommand = new RelayCommand(_ => ClearDiscountSelection(), _ => HasDiscountSelection);

        LoadSupplierOptions();
    }

    public void RefreshLocalization()
    {
        LoadSupplierOptions();
        SelectTrader(SelectedTrader?.Id ?? Supplier?.Id ?? SupplierOptions.FirstOrDefault()?.Id, loadData: SupplierOptions.Count > 0);
    }

    public void LoadForSupplier(SupplierListItem supplier)
    {
        LoadSupplierOptions();
        SelectTrader(supplier.Id, loadData: true);
    }

    public void LoadAll()
    {
        LoadSupplierOptions();
        SelectTrader(SelectedTrader?.Id ?? Supplier?.Id ?? AllTradersId, loadData: SupplierOptions.Count > 0);
    }

    private void LoadSupplierOptions()
    {
        var selectedTraderId = SelectedTrader?.Id ?? Supplier?.Id ?? AllTradersId;
        SupplierOptions.Clear();
        SupplierOptions.Add(new SupplierListItem
        {
            Id = AllTradersId,
            Name = UiText.L("LblAllSuppliers")
        });

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

        _isUpdatingSelection = true;
        try
        {
            SelectedTrader = SupplierOptions.FirstOrDefault(s => s.Id == selectedTraderId) ?? SupplierOptions.FirstOrDefault();
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    public void LoadTraderData(int traderId)
    {
        _ = LoadTraderDataAsync(traderId);
    }

    private async Task LoadTraderDataAsync(int traderId)
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            ToastService.ShowWarning(UiText.L("MsgFromBeforeTo"));
            return;
        }

        await RunBusyAsync(UiText.L("MsgLoadingTraderDetails"), async () =>
        {
            var selectedTrader = traderId == AllTradersId ? null : SupplierOptions.FirstOrDefault(s => s.Id == traderId);
            if (traderId != AllTradersId && selectedTrader == null)
            {
                Supplier = null;
                ResetViewState();
                return;
            }

            var requestedTransactionsPage = TransactionsCurrentPage;
            var requestedDiscountsPage = DiscountsCurrentPage;

            var pageData = await Task.Run(() =>
            {
                var supplierLookup = AppServices.SupplierService.GetSuppliers().ToDictionary(s => s.Id, s => s.Name);
                int? supplierId = traderId == AllTradersId ? null : traderId;
                var transactionsPage = AppServices.TransactionService.GetTransactionsPage(supplierId, FromDate, ToDate, requestedTransactionsPage, PageSize);
                var discountsPage = AppServices.DiscountService.GetDiscountsPage(supplierId, FromDate, ToDate, requestedDiscountsPage, PageSize);
                var summary = supplierId.HasValue
                    ? AppServices.TransactionService.GetSummary(supplierId.Value, FromDate, ToDate)
                    : AppServices.TransactionService.GetSummaryAll(FromDate, ToDate);
                return (supplierLookup, transactionsPage, discountsPage, summary);
            });

            int? supplierId = traderId == AllTradersId ? null : traderId;

            var effectiveTransactionsPage = pageData.transactionsPage;
            var transactionsTotalPages = Math.Max(effectiveTransactionsPage.TotalPages, 1);
            if (requestedTransactionsPage > transactionsTotalPages)
            {
                TransactionsCurrentPage = transactionsTotalPages;
                effectiveTransactionsPage = await Task.Run(() => AppServices.TransactionService.GetTransactionsPage(supplierId, FromDate, ToDate, TransactionsCurrentPage, PageSize));
            }

            var effectiveDiscountsPage = pageData.discountsPage;
            var discountsTotalPages = Math.Max(effectiveDiscountsPage.TotalPages, 1);
            if (requestedDiscountsPage > discountsTotalPages)
            {
                DiscountsCurrentPage = discountsTotalPages;
                effectiveDiscountsPage = await Task.Run(() => AppServices.DiscountService.GetDiscountsPage(supplierId, FromDate, ToDate, DiscountsCurrentPage, PageSize));
            }

            Supplier = selectedTrader;
            SelectedTransaction = null;

            TransactionsTotalCount = effectiveTransactionsPage.TotalCount;
            TransactionsTotalPages = Math.Max(effectiveTransactionsPage.TotalPages, 1);
            DiscountsTotalCount = effectiveDiscountsPage.TotalCount;
            DiscountsTotalPages = Math.Max(effectiveDiscountsPage.TotalPages, 1);

            Transactions.Clear();
            foreach (var transaction in effectiveTransactionsPage.Items)
            {
                Transactions.Add(new TransactionRow
                {
                    Id = transaction.Id,
                    SupplierId = transaction.SupplierId,
                    SupplierName = pageData.supplierLookup.TryGetValue(transaction.SupplierId, out var supplierName) ? supplierName : string.Empty,
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

            Discounts.Clear();
            foreach (var discount in effectiveDiscountsPage.Items)
            {
                Discounts.Add(new DiscountListItem
                {
                    Id = discount.Id,
                    SupplierId = discount.SupplierId,
                    SupplierName = pageData.supplierLookup.TryGetValue(discount.SupplierId, out var supplierName) ? supplierName : string.Empty,
                    Type = discount.Type,
                    Amount = discount.Amount,
                    Notes = discount.Notes ?? string.Empty,
                    CreatedAt = discount.CreatedAt,
                    UpdatedAt = discount.UpdatedAt
                });
            }

            ApplySummary(pageData.summary);
            OnPropertyChanged(nameof(HasTransactions));
            OnPropertyChanged(nameof(HasDiscounts));
            OnPropertyChanged(nameof(TransactionsRowsCountLabel));
            OnPropertyChanged(nameof(DiscountsRowsCountLabel));
            RefreshTransactionSelectionState();
            RefreshDiscountSelectionState();
        }, UiText.L("MsgGenericError"));
    }

    public void SetVisibleTransactionSelection(bool isSelected)
    {
        foreach (var transaction in Transactions)
        {
            transaction.IsSelected = isSelected;
        }

        RefreshTransactionSelectionState();
    }

    public void SetVisibleDiscountSelection(bool isSelected)
    {
        foreach (var discount in Discounts)
        {
            discount.IsSelected = isSelected;
        }

        RefreshDiscountSelectionState();
    }

    private async void AddTransaction()
    {
        if (Supplier == null)
        {
            return;
        }

        var defaults = AppServices.PricingSettingsService.GetLatest();
        var dialog = new Views.TransactionWindow(
            Supplier.Id,
            SupplierOptions.Where(s => s.Id != AllTradersId).ToList(),
            defaults.DefaultManufacturingPerGram,
            defaults.DefaultImprovementPerGram);

        if (dialog.ShowDialog() != true)
        {
            return;
        }

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
            await Task.Run(() => AppServices.TransactionService.AddTransaction(
                Supplier.Id,
                transactionDate,
                transactionCategory,
                originalWeight,
                itemName,
                originalKarat,
                manufacturingPerGram,
                improvementPerGram,
                notes));

            ToastService.ShowSuccess(UiText.L("MsgTransactionSaved"));
            await LoadTraderDataAsync(SelectedTrader?.Id ?? Supplier.Id);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            FileLogService.LogError("Add transaction from trader details failed", ex);
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
        }
    }

    private async void AddDiscount()
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

        var discountType = dialog.DiscountType;
        var amount = dialog.Amount;
        var notes = dialog.Notes;

        try
        {
            await Task.Run(() => AppServices.DiscountService.AddDiscount(Supplier.Id, discountType, amount, notes, FromDate, ToDate));
            ToastService.ShowSuccess(UiText.L("MsgDiscountSaved"));
            await LoadTraderDataAsync(SelectedTrader?.Id ?? Supplier.Id);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            FileLogService.LogError("Add discount from trader details failed", ex);
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
        }
    }

    private void ViewTransactionRow(TransactionRow? transaction)
    {
        if (transaction == null)
        {
            return;
        }

        var dialog = new Views.TransactionWindow(CreateTransactionListItem(transaction), SupplierOptions.Where(s => s.Id != AllTradersId).ToList(), isReadOnly: true);
        dialog.ShowDialog();
    }

    private async void EditTransaction(object? parameter)
    {
        var transaction = GetEditableTransaction(parameter);
        if (transaction == null)
        {
            return;
        }

        var dialog = new Views.TransactionWindow(CreateTransactionListItem(transaction), SupplierOptions.Where(s => s.Id != AllTradersId).ToList());
        if (dialog.ShowDialog() != true)
        {
            return;
        }

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
            await Task.Run(() => AppServices.TransactionService.UpdateTransaction(
                transaction.Id,
                transaction.SupplierId,
                transactionDate,
                transactionCategory,
                originalWeight,
                itemName,
                originalKarat,
                manufacturingPerGram,
                improvementPerGram,
                notes));
            ToastService.ShowSuccess(UiText.L("MsgTransactionUpdated", UiText.L("MsgTransactionSaved")));
            await LoadTraderDataAsync(SelectedTrader?.Id ?? Supplier?.Id ?? AllTradersId);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            FileLogService.LogError("Edit transaction from trader details failed", ex);
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
        }
    }

    private async void DeleteTransactions(object? parameter)
    {
        var deleteTargets = GetTransactionDeleteTargets(parameter);
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
            await Task.Run(() =>
            {
                foreach (var target in deleteTargets)
                {
                    AppServices.TransactionService.DeleteTransaction(target.Id);
                }
            });
            ToastService.ShowSuccess(deleteTargets.Count == 1
                ? UiText.L("MsgTransactionDeleted", "Transaction deleted successfully.")
                : UiText.Format("MsgTransactionsDeleted", deleteTargets.Count));
            await LoadTraderDataAsync(SelectedTrader?.Id ?? Supplier?.Id ?? AllTradersId);
        }
        catch (InvalidOperationException ex)
        {
            FileLogService.LogError("Delete transaction from trader details failed", ex);
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
        }
    }

    private void ViewDiscountRow(DiscountListItem? discount)
    {
        if (discount == null)
        {
            return;
        }

        var notes = string.IsNullOrWhiteSpace(discount.Notes) ? "-" : discount.Notes;
        System.Windows.MessageBox.Show(
            $"{UiText.L("LblDate")}: {discount.CreatedAt:yyyy/MM/dd HH:mm}{Environment.NewLine}{UiText.L("LblType")}: {discount.Type}{Environment.NewLine}{UiText.L("LblAmount")}: {discount.Amount:0.####}{Environment.NewLine}{UiText.L("LblNotes")}: {notes}",
            $"{discount.Type} - {discount.Amount:0.####}",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private async void EditDiscount(object? parameter)
    {
        var discount = GetEditableDiscount(parameter);
        if (discount == null)
        {
            return;
        }

        var dialog = new Views.DiscountWindow(discount);
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var discountType = dialog.DiscountType;
        var amount = dialog.Amount;
        var notes = dialog.Notes;

        try
        {
            await Task.Run(() => AppServices.DiscountService.UpdateDiscount(discount.Id, discount.SupplierId, discountType, amount, notes, FromDate, ToDate));
            ToastService.ShowSuccess(UiText.L("MsgDiscountUpdated", UiText.L("MsgDiscountSaved")));
            await LoadTraderDataAsync(SelectedTrader?.Id ?? Supplier?.Id ?? AllTradersId);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            FileLogService.LogError("Edit discount from trader details failed", ex);
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
        }
    }

    private async void DeleteDiscounts(object? parameter)
    {
        var deleteTargets = GetDiscountDeleteTargets(parameter);
        if (deleteTargets.Count == 0)
        {
            return;
        }

        var message = deleteTargets.Count == 1
            ? UiText.L("MsgDeleteSingleRecordConfirm")
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
            await Task.Run(() =>
            {
                foreach (var target in deleteTargets)
                {
                    AppServices.DiscountService.DeleteDiscount(target.Id);
                }
            });
            ToastService.ShowSuccess(deleteTargets.Count == 1
                ? UiText.L("MsgDiscountDeleted", "Discount deleted successfully.")
                : UiText.Format("MsgDiscountsDeleted", deleteTargets.Count));
            await LoadTraderDataAsync(SelectedTrader?.Id ?? Supplier?.Id ?? AllTradersId);
        }
        catch (InvalidOperationException ex)
        {
            FileLogService.LogError("Delete discount from trader details failed", ex);
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
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

    private void ResetViewState()
    {
        Transactions.Clear();
        Discounts.Clear();
        ApplySummary(new TraderSummary());
        TransactionsTotalCount = 0;
        DiscountsTotalCount = 0;
        TransactionsTotalPages = 1;
        DiscountsTotalPages = 1;
        SelectedTransaction = null;
        SelectedDiscount = null;
        OnPropertyChanged(nameof(HasTransactions));
        OnPropertyChanged(nameof(HasDiscounts));
        OnPropertyChanged(nameof(TransactionsRowsCountLabel));
        OnPropertyChanged(nameof(DiscountsRowsCountLabel));
        RefreshTransactionSelectionState();
        RefreshDiscountSelectionState();
    }

    private void ResetPages()
    {
        TransactionsCurrentPage = 1;
        DiscountsCurrentPage = 1;
    }

    private void SelectTrader(int? traderId, bool loadData = false)
    {
        var trader = traderId.HasValue ? SupplierOptions.FirstOrDefault(s => s.Id == traderId.Value) : null;
        _isUpdatingSelection = true;
        try
        {
            SelectedTrader = trader;
        }
        finally
        {
            _isUpdatingSelection = false;
        }

        if (loadData && trader != null)
        {
            LoadTraderData(trader.Id);
        }
    }

    private void ChangeTransactionsPage(int delta)
    {
        TransactionsCurrentPage = Math.Clamp(TransactionsCurrentPage + delta, 1, TransactionsTotalPages);
        LoadTraderData(SelectedTrader?.Id ?? AllTradersId);
    }

    private void ChangeDiscountsPage(int delta)
    {
        DiscountsCurrentPage = Math.Clamp(DiscountsCurrentPage + delta, 1, DiscountsTotalPages);
        LoadTraderData(SelectedTrader?.Id ?? AllTradersId);
    }

    private void GoToTransactionsPage()
    {
        TransactionsCurrentPage = Math.Clamp(TransactionsCurrentPage, 1, TransactionsTotalPages);
        LoadTraderData(SelectedTrader?.Id ?? AllTradersId);
    }

    private void GoToDiscountsPage()
    {
        DiscountsCurrentPage = Math.Clamp(DiscountsCurrentPage, 1, DiscountsTotalPages);
        LoadTraderData(SelectedTrader?.Id ?? AllTradersId);
    }

    private TransactionListItem CreateTransactionListItem(TransactionRow transaction)
    {
        return new TransactionListItem
        {
            Id = transaction.Id,
            SupplierId = transaction.SupplierId,
            SupplierName = transaction.SupplierName,
            Date = transaction.Date,
            Type = transaction.Type,
            Category = transaction.Category,
            OriginalWeight = transaction.OriginalWeight,
            ItemName = transaction.ItemName,
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
    }

    private void ClearTransactionSelection()
    {
        foreach (var transaction in Transactions)
        {
            transaction.IsSelected = false;
        }

        SelectedTransaction = null;
        RefreshTransactionSelectionState();
    }

    private void ClearDiscountSelection()
    {
        foreach (var discount in Discounts)
        {
            discount.IsSelected = false;
        }

        SelectedDiscount = null;
        RefreshDiscountSelectionState();
    }

    private void OnTransactionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<TransactionRow>())
            {
                item.PropertyChanged -= OnTransactionPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<TransactionRow>())
            {
                item.PropertyChanged += OnTransactionPropertyChanged;
            }
        }
    }

    private void OnDiscountsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<DiscountListItem>())
            {
                item.PropertyChanged -= OnDiscountPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<DiscountListItem>())
            {
                item.PropertyChanged += OnDiscountPropertyChanged;
            }
        }
    }

    private void OnTransactionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransactionRow.IsSelected))
        {
            RefreshTransactionSelectionState();
        }
    }

    private void OnDiscountPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DiscountListItem.IsSelected))
        {
            RefreshDiscountSelectionState();
        }
    }

    private TransactionRow? GetEditableTransaction(object? parameter)
    {
        if (parameter is TransactionRow transaction)
        {
            return transaction;
        }

        var checkedTransactions = Transactions.Where(item => item.IsSelected).ToList();
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

    private List<TransactionRow> GetTransactionDeleteTargets(object? parameter)
    {
        if (parameter is TransactionRow transaction)
        {
            return [transaction];
        }

        var checkedTransactions = Transactions.Where(item => item.IsSelected).ToList();
        if (checkedTransactions.Count > 0)
        {
            return checkedTransactions;
        }

        return SelectedTransaction == null ? [] : [SelectedTransaction];
    }

    private DiscountListItem? GetEditableDiscount(object? parameter)
    {
        if (parameter is DiscountListItem discount)
        {
            return discount;
        }

        var checkedDiscounts = Discounts.Where(item => item.IsSelected).ToList();
        if (checkedDiscounts.Count == 1)
        {
            return checkedDiscounts[0];
        }

        if (checkedDiscounts.Count > 1)
        {
            return null;
        }

        return SelectedDiscount;
    }

    private List<DiscountListItem> GetDiscountDeleteTargets(object? parameter)
    {
        if (parameter is DiscountListItem discount)
        {
            return [discount];
        }

        var checkedDiscounts = Discounts.Where(item => item.IsSelected).ToList();
        if (checkedDiscounts.Count > 0)
        {
            return checkedDiscounts;
        }

        return SelectedDiscount == null ? [] : [SelectedDiscount];
    }

    private void RefreshTransactionSelectionState()
    {
        OnPropertyChanged(nameof(AreAllVisibleTransactionsSelected));
        OnPropertyChanged(nameof(CheckedTransactionsCount));
        OnPropertyChanged(nameof(EffectiveTransactionsSelectedCount));
        OnPropertyChanged(nameof(TransactionsSelectedCountLabel));
        OnPropertyChanged(nameof(HasTransactionSelection));
        EditTransactionCommand.RaiseCanExecuteChanged();
        DeleteTransactionCommand.RaiseCanExecuteChanged();
        EditTransactionRowCommand.RaiseCanExecuteChanged();
        DeleteTransactionRowCommand.RaiseCanExecuteChanged();
        ClearTransactionSelectionCommand.RaiseCanExecuteChanged();
    }

    private void RefreshDiscountSelectionState()
    {
        OnPropertyChanged(nameof(AreAllVisibleDiscountsSelected));
        OnPropertyChanged(nameof(CheckedDiscountsCount));
        OnPropertyChanged(nameof(EffectiveDiscountsSelectedCount));
        OnPropertyChanged(nameof(DiscountsSelectedCountLabel));
        OnPropertyChanged(nameof(HasDiscountSelection));
        EditDiscountCommand.RaiseCanExecuteChanged();
        DeleteDiscountCommand.RaiseCanExecuteChanged();
        EditDiscountRowCommand.RaiseCanExecuteChanged();
        DeleteDiscountRowCommand.RaiseCanExecuteChanged();
        ClearDiscountSelectionCommand.RaiseCanExecuteChanged();
    }
}
