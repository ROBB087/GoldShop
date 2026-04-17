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
    private int _transactionCount;
    private int _discountCount;
    private decimal _transactionsManufacturingTotal;
    private decimal _transactionsImprovementTotal;

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

    public ObservableCollection<StatementPreviewRow> Rows { get; } = new();

    public string TotalWeightDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TotalGoldDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TransactionCountDisplay => _transactionCount.ToString("0");
    public string DiscountCountDisplay => _discountCount.ToString("0");
    public string TotalManufacturingDisplay => $"{_transactionsManufacturingTotal:0.##}";
    public string TotalImprovementDisplay => $"{_transactionsImprovementTotal:0.##}";
    public string NetTotalDisplay => $"{(_summary.FinalManufacturing + _summary.FinalImprovement):0.##}";

    public RelayCommand GenerateCommand { get; }

    public ModernStatementPreviewViewModel(int supplierId, string supplierName, DateTime? fromDate, DateTime? toDate)
    {
        _supplierId = supplierId;
        SupplierName = supplierName;
        var supplier = AppServices.SupplierService.GetSupplier(supplierId);
        SupplierPhone = !string.IsNullOrWhiteSpace(supplier?.Phone)
            ? supplier.Phone
            : supplier?.WorkerPhone;
        _fromDate = fromDate ?? DateTime.Today;
        _toDate = toDate ?? DateTime.Today;
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
        var discounts = AppServices.DiscountService.GetDiscounts(_supplierId, from, to);
        _summary = AppServices.TransactionService.GetSummary(_supplierId, from, to);
        _transactionCount = transactions.Count;
        _discountCount = discounts.Count;
        _transactionsManufacturingTotal = transactions.Sum(transaction => transaction.TotalManufacturing);
        _transactionsImprovementTotal = transactions.Sum(transaction => transaction.TotalImprovement);

        foreach (var transaction in transactions.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id))
        {
            Rows.Add(new StatementPreviewRow
            {
                Date = transaction.Date,
                Type = FormatType(transaction),
                Weight = transaction.Equivalent21,
                Item = transaction.ItemName ?? string.Empty,
                Manufacturing = transaction.TotalManufacturing,
                Improvement = transaction.TotalImprovement
            });
        }

        OnPropertyChanged(nameof(CurrentDateDisplay));
        OnPropertyChanged(nameof(TotalWeightDisplay));
        OnPropertyChanged(nameof(TotalGoldDisplay));
        OnPropertyChanged(nameof(TransactionCountDisplay));
        OnPropertyChanged(nameof(DiscountCountDisplay));
        OnPropertyChanged(nameof(TotalManufacturingDisplay));
        OnPropertyChanged(nameof(TotalImprovementDisplay));
        OnPropertyChanged(nameof(NetTotalDisplay));
    }

    public IReadOnlyList<SupplierTransaction> GetTransactions()
        => AppServices.TransactionService.GetTransactions(_supplierId, FromDate.Date, ToDate.Date);

    public TraderSummary GetSummary() => _summary;

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
}
