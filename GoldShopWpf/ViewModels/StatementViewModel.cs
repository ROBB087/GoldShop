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
    private int _transactionCount;
    private int _discountCount;
    private decimal _transactionsManufacturingTotal;
    private decimal _transactionsImprovementTotal;

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

    public string SupplierNameDisplay => SelectedSupplier?.Name ?? UiText.L("LblNoSupplier");
    public string CurrentDateDisplay => DateTime.Now.ToString("yyyy/MM/dd hh:mm tt");
    public string TotalWeightDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TotalGoldDisplay => $"{_summary.TotalGold21:0.####} {UiText.L("LblWeightUnit")}";
    public string TransactionCountDisplay => _transactionCount.ToString("0");
    public string DiscountCountDisplay => _discountCount.ToString("0");
    public string TotalManufacturingDisplay => $"{_transactionsManufacturingTotal:0.##}";
    public string TotalImprovementDisplay => $"{_transactionsImprovementTotal:0.##}";
    public string NetTotalDisplay => $"{(_summary.FinalManufacturing + _summary.FinalImprovement):0.##}";

    public RelayCommand GenerateCommand { get; }
    public RelayCommand PrintCommand { get; }

    public StatementViewModel()
    {
        SupplierChangeNotifier.SuppliersChanged += OnSuppliersChanged;
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
            RefreshPreview();
            return;
        }

        var from = FromDate.Date;
        var to = ToDate.Date;
        var transactions = AppServices.TransactionService.GetTransactions(SelectedSupplier.Id, from, to);
        var discounts = AppServices.DiscountService.GetDiscounts(SelectedSupplier.Id, from, to);
        _summary = AppServices.TransactionService.GetSummary(SelectedSupplier.Id, from, to);
        _transactionCount = transactions.Count;
        _discountCount = discounts.Count;
        _transactionsManufacturingTotal = transactions.Sum(transaction => transaction.TotalManufacturing);
        _transactionsImprovementTotal = transactions.Sum(transaction => transaction.TotalImprovement);
        Rows.Clear();

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

        var lines = new List<string>
        {
            UiText.L("ReceiptTitle"),
            $"{UiText.L("LblTrader")}: {SelectedSupplier.Name}",
            $"{UiText.L("ReceiptDateRange")}: {FromDate:yyyy/MM/dd} - {ToDate:yyyy/MM/dd}",
            new string('=', 72),
            $"{UiText.L("LblDate"),-12} {UiText.L("LblType"),-18} {UiText.L("LblEquivalent21"),12} {UiText.L("LblTotalManufacturing"),12} {UiText.L("LblTotalImprovement"),12}",
            new string('-', 72)
        };

        foreach (var transaction in transactions)
        {
            lines.Add(
                $"{transaction.Date:yyyy/MM/dd,-12} {FormatType(transaction),-18} {FormatNumber(transaction.Equivalent21, UiText.L("LblWeightUnit")),12} {FormatNumber(transaction.TotalManufacturing, string.Empty),12} {FormatNumber(transaction.TotalImprovement, string.Empty),12}");
        }

        lines.Add(new string('=', 72));
        lines.Add(UiText.L("ReceiptSummary"));
        lines.Add($"{UiText.L("LblTotalGold21")}: {FormatNumber(_summary.TotalGold21, UiText.L("LblWeightUnit"))}");
        lines.Add($"{UiText.L("LblTotalManufacturing")}: {FormatNumber(_transactionsManufacturingTotal, string.Empty)}");
        lines.Add($"{UiText.L("LblTotalImprovement")}: {FormatNumber(_transactionsImprovementTotal, string.Empty)}");
        lines.Add($"{UiText.L("LblNetTotalReport")}: {FormatNumber(_summary.FinalManufacturing + _summary.FinalImprovement, string.Empty)}");
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

    private void PrintStatement()
    {
        var window = new Views.ModernStatementWindow(
            SelectedSupplier?.Id ?? 0,
            SelectedSupplier?.Name ?? string.Empty,
            FromDate,
            ToDate);
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
    }
}
