using System.Collections.ObjectModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class StatementViewModel : ViewModelBase
{
    private SupplierListItem? _selectedSupplier;
    private DateTime _fromDate = DateTime.Today.AddMonths(-1);
    private DateTime _toDate = DateTime.Today;
    private string _statementText = string.Empty;
    private TraderSummary _summary = new();

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
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
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
    public string TotalManufacturingDisplay => $"{_summary.TotalManufacturing:0.##}";
    public string TotalImprovementDisplay => $"{_summary.TotalImprovement:0.##}";

    public RelayCommand GenerateCommand { get; }
    public RelayCommand PrintCommand { get; }

    public StatementViewModel()
    {
        GenerateCommand = new RelayCommand(_ => GenerateStatement());
        PrintCommand = new RelayCommand(_ => PrintStatement());
        LoadSuppliers();
    }

    private void LoadSuppliers()
    {
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

        SelectedSupplier ??= Suppliers.FirstOrDefault();
        GenerateStatement();
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

        var transactions = AppServices.TransactionService.GetTransactions(SelectedSupplier.Id, FromDate.Date, ToDate.Date);
        _summary = AppServices.TransactionService.GetSummary(SelectedSupplier.Id, FromDate.Date, ToDate.Date);
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
        lines.Add($"{UiText.L("LblTotalManufacturing")}: {FormatNumber(_summary.TotalManufacturing, string.Empty)}");
        lines.Add($"{UiText.L("LblTotalImprovement")}: {FormatNumber(_summary.TotalImprovement, string.Empty)}");
        lines.Add($"{UiText.L("LblManufacturingDiscounts")}: {FormatNumber(_summary.ManufacturingDiscounts, string.Empty)}");
        lines.Add($"{UiText.L("LblImprovementDiscounts")}: {FormatNumber(_summary.ImprovementDiscounts, string.Empty)}");
        StatementText = string.Join(Environment.NewLine, lines);
        RefreshPreview();
    }

    private static string FormatNumber(decimal value, string suffix)
        => string.IsNullOrWhiteSpace(suffix) ? $"{value:0.00}" : $"{value:0.00} {suffix}";

    private static string FormatType(SupplierTransaction transaction)
    {
        return transaction.Category switch
        {
            TransactionCategories.GoldOutbound => UiText.L("LblDebit"),
            TransactionCategories.GoldReceipt => UiText.L("LblCredit"),
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
        OnPropertyChanged(nameof(TotalManufacturingDisplay));
        OnPropertyChanged(nameof(TotalImprovementDisplay));
    }
}
