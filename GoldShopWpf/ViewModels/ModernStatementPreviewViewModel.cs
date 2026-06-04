using System.Collections.ObjectModel;
using System.Windows;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class ModernStatementPreviewViewModel : ViewModelBase
{
    private readonly int _supplierId;
    private DateTime _fromDate;
    private DateTime _toDate;
    private TraderSummary _summary = new();
    private TraderSummary _overallSummary = new();
    private int _transactionCount;
    private int _discountCount;
    private bool _showNotesInTable;

    public string SupplierName { get; }
    public string? SupplierPhone { get; }
    public string ReportTitle => UiText.L("ReceiptTitle");
    public string CurrentDateDisplay => DateTime.Now.ToString("yyyy/MM/dd hh:mm tt");

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value.Date))
            {
                Load();
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
                Load();
            }
        }
    }

    public bool ShowNotesInTable
    {
        get => _showNotesInTable;
        set
        {
            if (SetProperty(ref _showNotesInTable, value))
            {
                Load();
            }
        }
    }

    public ObservableCollection<StatementPreviewRow> Rows { get; } = new();

    public string TotalWeightDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TotalGoldDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TransactionCountDisplay => _transactionCount.ToString("0");
    public string DiscountCountDisplay => _discountCount.ToString("0");
    public string TotalManufacturingDisplay => FormatCurrency(_summary.FinalManufacturing);
    public string TotalImprovementDisplay => FormatCurrency(_summary.FinalImprovement);
    public string NetTotalDisplay => FormatCurrency(_summary.FinalManufacturing + _summary.FinalImprovement);
    public string SummaryPeriodTitle => $"ملخص الفترة: من {FromDate:yyyy/MM/dd} إلى {ToDate:yyyy/MM/dd}";
    public string OverallSummaryTitle => "الملخص العام";
    public string OverallTotalGoldDisplay => $"{_overallSummary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string OverallTotalManufacturingDisplay => FormatCurrency(_overallSummary.FinalManufacturing);
    public string OverallTotalImprovementDisplay => FormatCurrency(_overallSummary.FinalImprovement);
    public string OverallNetTotalDisplay => FormatCurrency(_overallSummary.FinalManufacturing + _overallSummary.FinalImprovement);

    public RelayCommand GenerateCommand { get; }

    public ModernStatementPreviewViewModel(int supplierId, string supplierName, DateTime? fromDate, DateTime? toDate, bool showNotesInTable = false)
    {
        _supplierId = supplierId;
        SupplierName = supplierName;
        var supplier = AppServices.SupplierService.GetSupplier(supplierId);
        SupplierPhone = !string.IsNullOrWhiteSpace(supplier?.Phone)
            ? supplier.Phone
            : supplier?.WorkerPhone;
        _fromDate = fromDate ?? DateTime.Today;
        _toDate = toDate ?? DateTime.Today;
        _showNotesInTable = showNotesInTable;
        FinancialDataChangeNotifier.DataChanged += OnFinancialDataChanged;
        GenerateCommand = new RelayCommand(_ => Load());
        Load();
    }

    public void Load()
    {
        if (FromDate > ToDate)
        {
            MessageBox.Show(UiText.L("MsgFromBeforeTo"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Rows.Clear();

        var from = FromDate.Date;
        var to = ToDate.Date;
        var transactions = AppServices.TransactionService.GetTransactions(_supplierId, from, to);
        var adjustments = AppServices.OpeningBalanceAdjustmentService.GetAdjustments(_supplierId, from, to);
        var discounts = AppServices.DiscountService.GetDiscounts(_supplierId, from, to);
        _summary = AppServices.TransactionService.GetSummary(_supplierId, from, to);
        _overallSummary = AppServices.TransactionService.GetSummary(_supplierId, null, null);
        _transactionCount = transactions.Count;
        _discountCount = discounts.Count;

        foreach (var row in BuildRows(transactions, adjustments, discounts))
        {
            Rows.Add(row);
        }

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

    public IReadOnlyList<StatementPreviewRow> GetRows()
        => Rows.ToList();

    public TraderSummary GetSummary() => _summary;

    public void Detach()
    {
        FinancialDataChangeNotifier.DataChanged -= OnFinancialDataChanged;
    }

    private void OnFinancialDataChanged()
    {
        Application.Current?.Dispatcher.Invoke(Load);
    }

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

    private static string FormatCurrency(decimal amount)
        => $"{amount:0.##} ج.م";

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
}
