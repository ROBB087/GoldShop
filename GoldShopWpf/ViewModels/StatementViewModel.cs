using System.Collections.ObjectModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class StatementViewModel : ViewModelBase
{
    private SupplierListItem? _selectedSupplier;
    private DateTime _fromDate = DateTime.Today;
    private DateTime _toDate = DateTime.Today;
    private string _statementText = string.Empty;
    private TraderSummary _summary = new();
    private TraderSummary _overallSummary = new();
    private int _transactionCount;
    private int _discountCount;
    private bool _showNotesInTable;

    public ObservableCollection<SupplierListItem> Suppliers { get; } = new();
    public ObservableCollection<StatementPreviewRow> Rows { get; } = new();

    public SupplierListItem? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                GenerateStatement();
            }
        }
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value.Date))
            {
                GenerateStatement();
            }
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value.Date))
            {
                GenerateStatement();
            }
        }
    }

    public string StatementText
    {
        get => _statementText;
        set => SetProperty(ref _statementText, value);
    }

    public bool ShowNotesInTable
    {
        get => _showNotesInTable;
        set
        {
            if (SetProperty(ref _showNotesInTable, value))
            {
                GenerateStatement();
            }
        }
    }

    public string SupplierNameDisplay => SelectedSupplier?.Name ?? UiText.L("LblNoSupplier");
    public string CurrentDateDisplay => DateTime.Now.ToString("yyyy/MM/dd hh:mm tt");
    public string TotalWeightDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TotalGoldDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TransactionCountDisplay => _transactionCount.ToString("0");
    public string DiscountCountDisplay => _discountCount.ToString("0");
    public string TotalManufacturingDisplay => $"{_summary.FinalManufacturing:0.##}";
    public string TotalImprovementDisplay => $"{_summary.FinalImprovement:0.##}";
    public string NetTotalDisplay => $"{(_summary.FinalManufacturing + _summary.FinalImprovement):0.##}";
    public string SummaryPeriodTitle => $"ملخص الفترة: من {FromDate:yyyy/MM/dd} إلى {ToDate:yyyy/MM/dd}";
    public string OverallSummaryTitle => "الملخص العام";
    public string OverallTotalGoldDisplay => $"{_overallSummary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string OverallTotalManufacturingDisplay => $"{_overallSummary.FinalManufacturing:0.##}";
    public string OverallTotalImprovementDisplay => $"{_overallSummary.FinalImprovement:0.##}";
    public string OverallNetTotalDisplay => $"{(_overallSummary.FinalManufacturing + _overallSummary.FinalImprovement):0.##}";

    public RelayCommand GenerateCommand { get; }
    public RelayCommand PrintCommand { get; }

    public StatementViewModel()
    {
        SupplierChangeNotifier.SuppliersChanged += OnSuppliersChanged;
        FinancialDataChangeNotifier.DataChanged += OnFinancialDataChanged;
        GenerateCommand = new RelayCommand(_ => GenerateStatement());
        PrintCommand = new RelayCommand(_ => PrintStatement());
        LoadSuppliers();
    }

    public void ReloadData()
    {
        LoadSuppliers();
    }

    private void LoadSuppliers()
    {
        var selectedSupplierId = SelectedSupplier?.Id;
        Suppliers.Clear();
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

        SelectedSupplier = selectedSupplierId.HasValue
            ? Suppliers.FirstOrDefault(supplier => supplier.Id == selectedSupplierId.Value) ?? Suppliers.FirstOrDefault()
            : Suppliers.FirstOrDefault();
        GenerateStatement();
    }

    private void OnSuppliersChanged()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(LoadSuppliers);
    }

    private void OnFinancialDataChanged()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(GenerateStatement);
    }

    private void GenerateStatement()
    {
        if (FromDate > ToDate)
        {
            System.Windows.MessageBox.Show(UiText.L("MsgFromBeforeTo"), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (SelectedSupplier == null)
        {
            StatementText = UiText.L("LblNoSupplier");
            Rows.Clear();
            _summary = new TraderSummary();
            _overallSummary = new TraderSummary();
            RefreshPreview();
            return;
        }

        var from = FromDate.Date;
        var to = ToDate.Date;
        var transactions = AppServices.TransactionService.GetTransactions(SelectedSupplier.Id, from, to);
        var adjustments = AppServices.OpeningBalanceAdjustmentService.GetAdjustments(SelectedSupplier.Id, from, to);
        var discounts = AppServices.DiscountService.GetDiscounts(SelectedSupplier.Id, from, to);
        _summary = AppServices.TransactionService.GetSummary(SelectedSupplier.Id, from, to);
        _overallSummary = AppServices.TransactionService.GetSummary(SelectedSupplier.Id, null, null);
        _transactionCount = transactions.Count;
        _discountCount = discounts.Count;
        Rows.Clear();

        foreach (var row in BuildRows(transactions, adjustments, discounts))
        {
            Rows.Add(row);
        }

        var lines = new List<string>
        {
            UiText.L("ReceiptTitle"),
            $"{UiText.L("LblTrader")}: {SelectedSupplier.Name}",
            $"{UiText.L("ReceiptDateRange")}: {FromDate:yyyy/MM/dd} - {ToDate:yyyy/MM/dd}",
            new string('=', 72),
            $"{UiText.L("LblDate"),-12} {UiText.L("LblType"),-18} {UiText.L("LblEquivalent21"),12} {UiText.L("LblTotalManufacturing"),12} {UiText.L("LblTotalImprovement"),12}",
            new string('-', 72)
        };

        foreach (var row in Rows)
        {
            lines.Add(
                $"{row.Date:yyyy/MM/dd,-12} {row.Type,-18} {FormatNumber(row.Weight, UiText.L("LblWeightUnit")),12} {FormatNumber(row.Manufacturing, string.Empty),12} {FormatNumber(row.Improvement, string.Empty),12}");
        }

        lines.Add(new string('=', 72));
        lines.Add(UiText.L("ReceiptSummary"));
        lines.Add($"{UiText.L("LblTotalGold21")}: {FormatNumber(_summary.TotalGold21, UiText.L("LblWeightUnit"))}");
        lines.Add($"{UiText.L("LblTotalManufacturing")}: {FormatNumber(_summary.FinalManufacturing, string.Empty)}");
        lines.Add($"{UiText.L("LblTotalImprovement")}: {FormatNumber(_summary.FinalImprovement, string.Empty)}");
        lines.Add($"{UiText.L("LblNetTotalReport")}: {FormatNumber(_summary.FinalManufacturing + _summary.FinalImprovement, string.Empty)}");
        lines.Add(string.Empty);
        lines.Add(OverallSummaryTitle);
        lines.Add($"{UiText.L("LblTotalGold21")}: {FormatNumber(_overallSummary.TotalGold21, UiText.L("LblWeightUnit"))}");
        lines.Add($"{UiText.L("LblTotalManufacturing")}: {FormatNumber(_overallSummary.FinalManufacturing, string.Empty)}");
        lines.Add($"{UiText.L("LblTotalImprovement")}: {FormatNumber(_overallSummary.FinalImprovement, string.Empty)}");
        lines.Add($"{UiText.L("LblNetTotalReport")}: {FormatNumber(_overallSummary.FinalManufacturing + _overallSummary.FinalImprovement, string.Empty)}");
        StatementText = string.Join(Environment.NewLine, lines);
        RefreshPreview();
    }

    private static string FormatNumber(decimal value, string suffix)
        => string.IsNullOrWhiteSpace(suffix) ? $"{value:0.00}" : $"{value:0.00} {suffix}";

    private static string FormatType(SupplierTransaction transaction)
    {
        return transaction.Category switch
        {
            TransactionCategories.GoldOutbound => UiText.L("LblGoldOutboundReport"),
            TransactionCategories.GoldReceipt => UiText.L("LblGoldReceiptReport"),
            TransactionCategories.FinishedGoldReceipt => UiText.L("LblFinishedGoldReceiptReport"),
            TransactionCategories.CashPayment => UiText.L("LblCashPaymentReport"),
            _ => transaction.Type.ToString()
        };
    }

    private static string GetDisplayItemName(string? itemName)
        => string.IsNullOrWhiteSpace(itemName) ? UiText.L("LblUnspecifiedItem") : itemName.Trim();

    private static string GetDisplayNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? UiText.L("LblNoNotesValue") : notes.Trim();

    private static IReadOnlyList<StatementPreviewRow> BuildRows(
        IReadOnlyList<SupplierTransaction> transactions,
        IReadOnlyList<OpeningBalanceAdjustment> adjustments,
        IReadOnlyList<DiscountRecord> discounts)
    {
        return transactions.Select(transaction => new StatementPreviewRow
            {
                SortId = transaction.Id,
                Date = transaction.Date,
                Type = FormatType(transaction),
                Weight = transaction.Equivalent21,
                Item = GetDisplayItemName(transaction.ItemName),
                Notes = GetDisplayNotes(transaction.Notes),
                Manufacturing = transaction.TotalManufacturing,
                Improvement = transaction.TotalImprovement
            })
            .Concat(adjustments.Select(adjustment => new StatementPreviewRow
            {
                SortId = adjustment.Id,
                Date = adjustment.AdjustmentDate,
                Type = adjustment.Type == OpeningBalanceAdjustmentType.Manufacturing
                    ? UiText.L("LblOpeningBalanceManufacturingAdjustment")
                    : UiText.L("LblOpeningBalanceImprovementAdjustment"),
                Weight = 0m,
                Item = adjustment.Type == OpeningBalanceAdjustmentType.Manufacturing
                    ? UiText.L("LblOpeningBalanceManufacturingAdjustment")
                    : UiText.L("LblOpeningBalanceImprovementAdjustment"),
                Notes = GetDisplayNotes(adjustment.Notes),
                Manufacturing = adjustment.Type == OpeningBalanceAdjustmentType.Manufacturing ? adjustment.Amount : 0m,
                Improvement = adjustment.Type == OpeningBalanceAdjustmentType.Improvement ? adjustment.Amount : 0m
            }))
            .Concat(discounts.Select(discount => new StatementPreviewRow
            {
                SortId = discount.Id,
                Date = discount.CreatedAt.Date,
                Type = discount.Type == DiscountType.Manufacturing
                    ? UiText.L("LblManufacturingDiscountEntry")
                    : UiText.L("LblImprovementDiscountEntry"),
                Weight = 0m,
                Item = string.Empty,
                Notes = GetDisplayNotes(discount.Notes),
                Manufacturing = discount.Type == DiscountType.Manufacturing ? -discount.Amount : 0m,
                Improvement = discount.Type == DiscountType.Improvement ? -discount.Amount : 0m
            }))
            .OrderByDescending(row => row.Date)
            .ThenByDescending(row => row.SortId)
            .ToList();
    }

    private void PrintStatement()
    {
        var window = new Views.ModernStatementWindow(
            SelectedSupplier?.Id ?? 0,
            SelectedSupplier?.Name ?? string.Empty,
            FromDate,
            ToDate,
            ShowNotesInTable);
        window.ShowDialog();
    }

    private void RefreshPreview()
    {
        OnPropertyChanged(nameof(SupplierNameDisplay));
        OnPropertyChanged(nameof(CurrentDateDisplay));
        OnPropertyChanged(nameof(TotalWeightDisplay));
        OnPropertyChanged(nameof(TotalGoldDisplay));
        OnPropertyChanged(nameof(TransactionCountDisplay));
        OnPropertyChanged(nameof(DiscountCountDisplay));
        OnPropertyChanged(nameof(TotalManufacturingDisplay));
        OnPropertyChanged(nameof(TotalImprovementDisplay));
        OnPropertyChanged(nameof(NetTotalDisplay));
        OnPropertyChanged(nameof(SummaryPeriodTitle));
        OnPropertyChanged(nameof(OverallSummaryTitle));
        OnPropertyChanged(nameof(OverallTotalGoldDisplay));
        OnPropertyChanged(nameof(OverallTotalManufacturingDisplay));
        OnPropertyChanged(nameof(OverallTotalImprovementDisplay));
        OnPropertyChanged(nameof(OverallNetTotalDisplay));
    }
}
