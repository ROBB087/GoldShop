using System.Collections.ObjectModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class SupplierDetailsViewModel : ViewModelBase
{
    private const int AllTradersId = 0;

    private SupplierListItem? _supplier;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _searchText = string.Empty;
    private TransactionRow? _selectedTransaction;
    private SupplierListItem? _selectedTrader;
    private decimal _totalGold21;
    private decimal _totalManufacturing;
    private decimal _totalImprovement;
    private decimal _manufacturingDiscounts;
    private decimal _improvementDiscounts;
    private bool _isUpdatingSelection;
    private List<TransactionRow> _allTransactions = [];
    private List<DiscountListItem> _allDiscounts = [];

    public ObservableCollection<SupplierListItem> SupplierOptions { get; } = new();
    public ObservableCollection<TransactionRow> Transactions { get; } = new();
    public ObservableCollection<DiscountListItem> Discounts { get; } = new();
    public bool? AreAllVisibleTransactionsSelected
    {
        get
        {
            if (Transactions.Count == 0)
            {
                return false;
            }

            var selectedCount = Transactions.Count(transaction => transaction.IsSelected);
            if (selectedCount == 0)
            {
                return false;
            }

            return selectedCount == Transactions.Count ? true : null;
        }
        set
        {
            foreach (var transaction in Transactions)
            {
                transaction.IsSelected = value == true;
            }

            RefreshSelectionState();
        }
    }

    public int CheckedTransactionCount => _allTransactions.Count(transaction => transaction.IsSelected);
    public string SelectedTransactionsLabel => UiText.Format("LblSelectedCount", CheckedTransactionCount);
    public bool HasCheckedTransactions => CheckedTransactionCount > 0;

    public SupplierListItem? SelectedTrader
    {
        get => _selectedTrader;
        set
        {
            if (SetProperty(ref _selectedTrader, value) && !_isUpdatingSelection)
            {
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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplySearchFilter();
            }
        }
    }

    public TransactionRow? SelectedTransaction
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
            }
        }
    }

    public decimal FinalManufacturing => TotalManufacturing - ManufacturingDiscounts;
    public decimal FinalImprovement => TotalImprovement - ImprovementDiscounts;
    public decimal TotalDiscounts => ManufacturingDiscounts + ImprovementDiscounts;

    public RelayCommand ApplyFilterCommand { get; }
    public RelayCommand ClearFilterCommand { get; }
    public RelayCommand AddTransactionCommand { get; }
    public RelayCommand AddDiscountCommand { get; }
    public RelayCommand EditTransactionCommand { get; }
    public RelayCommand DeleteTransactionCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand PrintStatementCommand { get; }
    public RelayCommand ViewTransactionRowCommand { get; }
    public RelayCommand EditTransactionRowCommand { get; }
    public RelayCommand DeleteTransactionRowCommand { get; }
    public RelayCommand ViewDiscountRowCommand { get; }
    public RelayCommand DeleteDiscountRowCommand { get; }

    public SupplierDetailsViewModel()
    {
        Transactions.CollectionChanged += OnTransactionsCollectionChanged;
        ApplyFilterCommand = new RelayCommand(_ =>
        {
            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        });
        ClearFilterCommand = new RelayCommand(_ =>
        {
            FromDate = null;
            ToDate = null;
            SearchText = string.Empty;
            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        });
        AddTransactionCommand = new RelayCommand(_ => AddTransaction(), _ => Supplier != null);
        AddDiscountCommand = new RelayCommand(_ => AddDiscount(), _ => Supplier != null);
        EditTransactionCommand = new RelayCommand(_ => EditTransaction(), _ => GetEditableTransaction() != null);
        DeleteTransactionCommand = new RelayCommand(_ => DeleteTransactions(), _ => GetCheckedTransactions().Count > 0);
        RefreshCommand = new RelayCommand(_ =>
        {
            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        }, _ => SelectedTrader != null);
        PrintStatementCommand = new RelayCommand(_ => PrintStatement(), _ => Supplier != null);
        ViewTransactionRowCommand = new RelayCommand(row => ViewTransactionRow(row as TransactionRow), row => row is TransactionRow);
        EditTransactionRowCommand = new RelayCommand(row => EditTransaction(row as TransactionRow), row => row is TransactionRow);
        DeleteTransactionRowCommand = new RelayCommand(row => DeleteTransactions(row as TransactionRow), row => row is TransactionRow);
        ViewDiscountRowCommand = new RelayCommand(row => ViewDiscountRow(row as DiscountListItem), row => row is DiscountListItem);
        DeleteDiscountRowCommand = new RelayCommand(row => DeleteDiscountRow(row as DiscountListItem), row => row is DiscountListItem);

        LoadSupplierOptions();
    }

    public void RefreshLocalization()
    {
        LoadSupplierOptions();
        SelectTrader(SelectedTrader?.Id ?? Supplier?.Id ?? SupplierOptions.FirstOrDefault()?.Id, loadData: SupplierOptions.Count > 0);
    }

    public void LoadForSupplier(SupplierListItem supplier)
    {
        FromDate ??= DateTime.Today.AddMonths(-1);
        ToDate ??= DateTime.Today;
        LoadSupplierOptions();
        SelectTrader(supplier.Id, loadData: true);
    }

    public void LoadAll()
    {
        FromDate ??= DateTime.Today.AddMonths(-1);
        ToDate ??= DateTime.Today;
        LoadSupplierOptions();
        SelectTrader(SelectedTrader?.Id ?? Supplier?.Id ?? AllTradersId, loadData: SupplierOptions.Count > 0);
    }

    public void SetVisibleTransactionSelection(bool isSelected)
    {
        foreach (var transaction in Transactions)
        {
            transaction.IsSelected = isSelected;
        }

        RefreshSelectionState();
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
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            System.Windows.MessageBox.Show(UiText.L("MsgFromBeforeTo"), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var suppliers = AppServices.SupplierService.GetSuppliers()
            .ToDictionary(
                s => s.Id,
                s => new SupplierListItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    Phone = s.Phone ?? string.Empty,
                    WorkerName = s.WorkerName ?? string.Empty,
                    WorkerPhone = s.WorkerPhone ?? string.Empty
                });

        if (traderId == AllTradersId)
        {
            Supplier = null;
            SelectTrader(AllTradersId);
            SelectedTransaction = null;

            _allTransactions = AppServices.TransactionService.GetTransactions(FromDate, ToDate)
                .Select(transaction => new TransactionRow
                {
                    Id = transaction.Id,
                    SupplierName = suppliers.TryGetValue(transaction.SupplierId, out var supplier) ? supplier.Name : string.Empty,
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
                })
                .ToList();

            _allDiscounts = AppServices.DiscountService.GetDiscounts(FromDate, ToDate)
                .Select(discount => new DiscountListItem
                {
                    Id = discount.Id,
                    SupplierName = suppliers.TryGetValue(discount.SupplierId, out var supplier) ? supplier.Name : string.Empty,
                    Type = discount.Type,
                    Amount = discount.Amount,
                    Notes = discount.Notes ?? string.Empty,
                    CreatedAt = discount.CreatedAt
                })
                .ToList();

            ApplySummary(AppServices.TransactionService.GetSummaryAll(FromDate, ToDate));
            ApplySearchFilter();
            return;
        }

        var trader = SupplierOptions.FirstOrDefault(s => s.Id == traderId);
        if (trader == null)
        {
            Supplier = null;
            ResetViewState();
            return;
        }

        Supplier = trader;
        SelectTrader(traderId);
        SelectedTransaction = null;

        _allTransactions = AppServices.TransactionService.GetTransactions(traderId, FromDate, ToDate)
            .Select(transaction => new TransactionRow
            {
                Id = transaction.Id,
                SupplierName = trader.Name,
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
            })
            .ToList();

        _allDiscounts = AppServices.DiscountService.GetDiscounts(traderId, FromDate, ToDate)
            .Select(discount => new DiscountListItem
            {
                Id = discount.Id,
                SupplierName = trader.Name,
                Type = discount.Type,
                Amount = discount.Amount,
                Notes = discount.Notes ?? string.Empty,
                CreatedAt = discount.CreatedAt
            })
            .ToList();

        ApplySummary(AppServices.TransactionService.GetSummary(traderId, FromDate, ToDate));
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        var selectedTransactionId = SelectedTransaction?.Id;

        Transactions.Clear();
        Discounts.Clear();

        var query = SearchText.Trim().ToLowerInvariant();

        foreach (var transaction in _allTransactions)
        {
            if (!string.IsNullOrWhiteSpace(query) &&
                !transaction.SupplierName.ToLowerInvariant().Contains(query) &&
                !transaction.ItemName.ToLowerInvariant().Contains(query) &&
                !transaction.Notes.ToLowerInvariant().Contains(query) &&
                !transaction.Traceability.ToLowerInvariant().Contains(query))
            {
                continue;
            }

            Transactions.Add(transaction);
        }

        foreach (var discount in _allDiscounts)
        {
            if (!string.IsNullOrWhiteSpace(query) &&
                !discount.SupplierName.ToLowerInvariant().Contains(query) &&
                !discount.Notes.ToLowerInvariant().Contains(query))
            {
                continue;
            }

            Discounts.Add(discount);
        }

        SelectedTransaction = selectedTransactionId.HasValue
            ? Transactions.FirstOrDefault(transaction => transaction.Id == selectedTransactionId.Value)
            : null;
        RefreshSelectionState();
    }

    private void AddTransaction()
    {
        if (Supplier == null)
        {
            return;
        }

        var defaults = AppServices.PricingSettingsService.GetLatest();
        var dialog = new Views.TransactionWindow(
            Supplier.Id,
            GetPopupSuppliers(),
            defaults.DefaultManufacturingPerGram,
            defaults.DefaultImprovementPerGram);
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            AppServices.TransactionService.AddTransaction(
                Supplier.Id,
                dialog.TransactionDate,
                dialog.TransactionCategory,
                dialog.OriginalWeight,
                dialog.ItemName,
                dialog.OriginalKarat,
                dialog.ManufacturingPerGram,
                dialog.ImprovementPerGram,
                dialog.Notes);
            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        }
        catch (ArgumentException ex)
        {
            System.Windows.MessageBox.Show(UiText.LocalizeException(ex.Message), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        }
        catch (ArgumentException ex)
        {
            System.Windows.MessageBox.Show(UiText.LocalizeException(ex.Message), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private void ViewTransactionRow(TransactionRow? transaction)
    {
        if (transaction == null)
        {
            return;
        }

        var item = CreateTransactionListItem(transaction);
        var dialog = new Views.TransactionWindow(item, GetPopupSuppliers(), isReadOnly: true);
        dialog.ShowDialog();
    }

    private void EditTransaction(TransactionRow? requestedTransaction = null)
    {
        var editableTransaction = requestedTransaction ?? GetEditableTransaction();
        if (editableTransaction == null)
        {
            return;
        }

        var item = CreateTransactionListItem(editableTransaction);

        var dialog = new Views.TransactionWindow(item, GetPopupSuppliers());
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            AppServices.TransactionService.UpdateTransaction(
                editableTransaction.Id,
                dialog.SupplierId,
                dialog.TransactionDate,
                dialog.TransactionCategory,
                dialog.OriginalWeight,
                dialog.ItemName,
                dialog.OriginalKarat,
                dialog.ManufacturingPerGram,
                dialog.ImprovementPerGram,
                dialog.Notes);
            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        }
        catch (ArgumentException ex)
        {
            System.Windows.MessageBox.Show(UiText.LocalizeException(ex.Message), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private void DeleteTransactions(TransactionRow? transaction = null)
    {
        var deleteTargets = transaction == null ? GetCheckedTransactions() : [transaction];
        if (deleteTargets.Count == 0)
        {
            return;
        }

        var message = GetDeleteConfirmationMessage(deleteTargets.Count);
        var result = System.Windows.MessageBox.Show(
            message,
            UiText.L("TitleConfirmDelete"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            foreach (var item in deleteTargets)
            {
                AppServices.TransactionService.DeleteTransaction(item.Id);
            }

            if (SelectedTrader != null)
            {
                LoadTraderData(SelectedTrader.Id);
            }
        }
    }

    private List<SupplierListItem> GetPopupSuppliers()
        => SupplierOptions.Where(s => s.Id != AllTradersId).ToList();

    private void ViewDiscountRow(DiscountListItem? discount)
    {
        if (discount == null)
        {
            return;
        }

        var title = $"{discount.Type} - {discount.Amount:0.####}";
        var notes = string.IsNullOrWhiteSpace(discount.Notes) ? "-" : discount.Notes;
        System.Windows.MessageBox.Show(
            $"التاريخ: {discount.CreatedAt:yyyy/MM/dd HH:mm}{Environment.NewLine}النوع: {discount.Type}{Environment.NewLine}القيمة: {discount.Amount:0.####}{Environment.NewLine}الملاحظات: {notes}",
            title,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private void DeleteDiscountRow(DiscountListItem? discount)
    {
        if (discount == null)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            UiText.L("MsgDeleteSingleRecordConfirm"),
            UiText.L("TitleConfirmDelete"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        AppServices.DiscountService.DeleteDiscount(discount.Id);
        if (SelectedTrader != null)
        {
            LoadTraderData(SelectedTrader.Id);
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
        SelectedTransaction = null;
        _allTransactions = [];
        _allDiscounts = [];
        Transactions.Clear();
        Discounts.Clear();
        ApplySummary(new TraderSummary());
        RefreshSelectionState();
    }

    private void SelectTrader(int? traderId, bool loadData = false)
    {
        var trader = traderId.HasValue
            ? SupplierOptions.FirstOrDefault(s => s.Id == traderId.Value)
            : null;

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
        else if (loadData)
        {
            Supplier = null;
            ResetViewState();
        }
    }

    private void OnTransactionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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

    private void OnTransactionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransactionRow.IsSelected))
        {
            RefreshSelectionState();
        }
    }

    private TransactionRow? GetEditableTransaction()
    {
        var checkedTransactions = GetCheckedTransactions();
        if (checkedTransactions.Count == 1)
        {
            return checkedTransactions[0];
        }

        if (checkedTransactions.Count > 1)
        {
            return null;
        }

        return SelectedTransaction?.IsSelected == true ? SelectedTransaction : null;
    }

    private List<TransactionRow> GetCheckedTransactions()
    {
        return Transactions.Where(transaction => transaction.IsSelected).ToList();
    }

    private void RefreshSelectionState()
    {
        OnPropertyChanged(nameof(AreAllVisibleTransactionsSelected));
        OnPropertyChanged(nameof(CheckedTransactionCount));
        OnPropertyChanged(nameof(SelectedTransactionsLabel));
        OnPropertyChanged(nameof(HasCheckedTransactions));
        AddTransactionCommand.RaiseCanExecuteChanged();
        AddDiscountCommand.RaiseCanExecuteChanged();
        EditTransactionCommand.RaiseCanExecuteChanged();
        DeleteTransactionCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
    }

    private string GetDeleteConfirmationMessage(int targetCount)
    {
        if (targetCount > 0 && targetCount == _allTransactions.Count && _allTransactions.Count > 0)
        {
            return UiText.L("MsgDeleteAllRecordsConfirm");
        }

        return targetCount == 1
            ? UiText.L("MsgDeleteSingleRecordConfirm")
            : UiText.Format("MsgDeleteSelectedRecordsConfirm", targetCount);
    }

    private TransactionListItem CreateTransactionListItem(TransactionRow transaction)
    {
        return new TransactionListItem
        {
            Id = transaction.Id,
            SupplierId = Supplier?.Id ?? SelectedTrader?.Id ?? 0,
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
}
