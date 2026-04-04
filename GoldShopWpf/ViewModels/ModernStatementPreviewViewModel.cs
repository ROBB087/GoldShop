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

    public string SupplierName { get; }
    public string ReportTitle => UiText.L("ReceiptTitle");
    public string CurrentDateDisplay => DateTime.Now.ToString("yyyy/MM/dd hh:mm tt");

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public ObservableCollection<StatementPreviewRow> Rows { get; } = new();

    public string TotalWeightDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TotalGoldDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TotalManufacturingDisplay => $"{_summary.TotalManufacturing:0.##}";
    public string TotalImprovementDisplay => $"{_summary.TotalImprovement:0.##}";

    public RelayCommand GenerateCommand { get; }

    public ModernStatementPreviewViewModel(int supplierId, string supplierName, DateTime? fromDate, DateTime? toDate)
    {
        _supplierId = supplierId;
        SupplierName = supplierName;
        _fromDate = fromDate ?? DateTime.Today.AddMonths(-1);
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

        var transactions = AppServices.TransactionService.GetTransactions(_supplierId, FromDate.Date, ToDate.Date);
        _summary = AppServices.TransactionService.GetSummary(_supplierId, FromDate.Date, ToDate.Date);

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
        OnPropertyChanged(nameof(TotalManufacturingDisplay));
        OnPropertyChanged(nameof(TotalImprovementDisplay));
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
            TransactionCategories.CashPayment => UiText.L("LblCashPaymentReport"),
            _ => transaction.Type.ToString()
        };
    }
}
