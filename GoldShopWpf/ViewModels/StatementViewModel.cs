using System.Collections.ObjectModel;
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
            System.Windows.MessageBox.Show("From date must be before To date.", "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (SelectedSupplier == null)
        {
            StatementText = "Select a trader to generate a statement.";
            return;
        }

        var transactions = AppServices.TransactionService.GetTransactions(SelectedSupplier.Id, FromDate.Date, ToDate.Date);
        var summary = AppServices.TransactionService.GetSummary(SelectedSupplier.Id, FromDate.Date, ToDate.Date);

        var lines = new List<string>
        {
            "Trader Statement",
            $"Trader: {SelectedSupplier.Name}",
            $"Date Range: {FromDate:yyyy-MM-dd} - {ToDate:yyyy-MM-dd}",
            new string('-', 150),
            string.Format("{0,-12} {1,-4} {2,10} {3,7} {4,10} {5,12} {6,12} {7,-28} {8,-16} {9,-16}",
                "Date", "Type", "Weight", "Karat", "Eq21", "Mfg Total", "Imp Total", "Traceability", "Created", "Updated"),
            new string('-', 150)
        };

        foreach (var transaction in transactions)
        {
            lines.Add(string.Format(
                "{0,-12} {1,-4} {2,10} {3,7} {4,10} {5,12} {6,12} {7,-28} {8,-16} {9,-16}",
                transaction.Date.ToString("yyyy-MM-dd"),
                transaction.Type,
                FormatNumber(transaction.OriginalWeight, "g"),
                $"{transaction.OriginalKarat}K",
                FormatNumber(transaction.Equivalent21, "g"),
                FormatNumber(transaction.TotalManufacturing, string.Empty),
                FormatNumber(transaction.TotalImprovement, string.Empty),
                Truncate(transaction.Description, 28),
                transaction.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                transaction.UpdatedAt.ToString("yyyy-MM-dd HH:mm")));
        }

        lines.Add(new string('-', 150));
        lines.Add($"Total Gold (21K): {FormatNumber(summary.TotalGold21, "g")}");
        lines.Add($"Total Manufacturing: {FormatNumber(summary.TotalManufacturing, string.Empty)}");
        lines.Add($"Total Improvement: {FormatNumber(summary.TotalImprovement, string.Empty)}");
        lines.Add($"Manufacturing Discounts: {FormatNumber(summary.ManufacturingDiscounts, string.Empty)}");
        lines.Add($"Improvement Discounts: {FormatNumber(summary.ImprovementDiscounts, string.Empty)}");
        lines.Add($"Final Manufacturing: {FormatNumber(summary.FinalManufacturing, string.Empty)}");
        lines.Add($"Final Improvement: {FormatNumber(summary.FinalImprovement, string.Empty)}");

        StatementText = string.Join(Environment.NewLine, lines);
    }

    private static string Truncate(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= length ? value : value[..(length - 3)] + "...";
    }

    private static string FormatNumber(decimal value, string suffix)
        => string.IsNullOrWhiteSpace(suffix) ? $"{value:0.00}" : $"{value:0.00} {suffix}";

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
