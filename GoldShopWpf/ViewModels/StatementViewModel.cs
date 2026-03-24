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

    public ObservableCollection<SupplierListItem> Suppliers { get; } = new();

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
            return;
        }

        var transactions = AppServices.TransactionService.GetTransactions(SelectedSupplier.Id, FromDate.Date, ToDate.Date);
        var summary = AppServices.TransactionService.GetSummary(SelectedSupplier.Id, FromDate.Date, ToDate.Date);

        var lines = new List<string>
        {
            UiText.L("ReceiptTitle"),
            $"{UiText.L("LblTrader")}: {SelectedSupplier.Name}",
            $"{UiText.L("ReceiptDateRange")}: {FromDate:yyyy-MM-dd} - {ToDate:yyyy-MM-dd}",
            new string('=', 72),
            $"{UiText.L("LblDate"),-12} {UiText.L("LblType"),-18} {UiText.L("LblWeight"),12} {UiText.L("LblAmount"),12}",
            new string('-', 72)
        };

        foreach (var transaction in transactions)
        {
            lines.Add(
                $"{transaction.Date:yyyy-MM-dd,-12} {FormatType(transaction),-18} {FormatNumber(transaction.OriginalWeight, UiText.L("LblWeightUnit")),12} {FormatNumber(transaction.TotalManufacturing + transaction.TotalImprovement, string.Empty),12}");
        }

        lines.Add(new string('=', 72));
        lines.Add(UiText.L("ReceiptSummary"));
        lines.Add($"{UiText.L("LblTotalGold21")}: {FormatNumber(summary.TotalGold21, UiText.L("LblWeightUnit"))}");
        lines.Add($"{UiText.L("LblTotalManufacturing")}: {FormatNumber(summary.TotalManufacturing, string.Empty)}");
        lines.Add($"{UiText.L("LblTotalImprovement")}: {FormatNumber(summary.TotalImprovement, string.Empty)}");
        lines.Add($"{UiText.L("LblManufacturingDiscounts")}: {FormatNumber(summary.ManufacturingDiscounts, string.Empty)}");
        lines.Add($"{UiText.L("LblImprovementDiscounts")}: {FormatNumber(summary.ImprovementDiscounts, string.Empty)}");
        lines.Add($"{UiText.L("LblFinalManufacturing")}: {FormatNumber(summary.FinalManufacturing, string.Empty)}");
        lines.Add($"{UiText.L("LblFinalImprovement")}: {FormatNumber(summary.FinalImprovement, string.Empty)}");

        StatementText = string.Join(Environment.NewLine, lines);
    }

    private static string FormatNumber(decimal value, string suffix)
        => string.IsNullOrWhiteSpace(suffix) ? $"{value:0.00}" : $"{value:0.00} {suffix}";

    private static string FormatType(SupplierTransaction transaction)
    {
        return transaction.Category switch
        {
            TransactionCategories.GoldOutbound => UiText.L("LblDebit"),
            TransactionCategories.GoldReceipt => UiText.L("LblCredit"),
            TransactionCategories.CashPayment => UiText.L("LblTransactionCategory"),
            _ => transaction.Type.ToString()
        };
    }

    private void PrintStatement()
    {
        var window = new Views.StatementWindow(
            SelectedSupplier?.Id ?? 0,
            SelectedSupplier?.Name ?? string.Empty,
            FromDate,
            ToDate);
        window.ShowDialog();
    }
}
