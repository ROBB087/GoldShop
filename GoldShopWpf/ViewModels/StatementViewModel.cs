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
                Phone = supplier.Phone ?? string.Empty
            });
        }

        SelectedSupplier ??= Suppliers.FirstOrDefault();
        GenerateStatement();
    }

    private void GenerateStatement()
    {
        if (SelectedSupplier == null)
        {
            StatementText = System.Windows.Application.Current.TryFindResource("LblNoSupplier")?.ToString() ?? string.Empty;
            return;
        }

        var transactions = AppServices.TransactionService.GetTransactions(SelectedSupplier.Id, FromDate.Date, ToDate.Date);

        var isArabic = GoldShopWpf.Services.LocalizationService.CurrentLanguage == "ar";
        var supplierLabel = isArabic ? "المورد" : "Supplier";
        var dateRangeLabel = isArabic ? "الفترة" : "Date Range";
        var finalBalanceLabel = isArabic ? "الرصيد النهائي" : "Final Balance";
        var dateHeader = System.Windows.Application.Current.TryFindResource("LblDate")?.ToString() ?? "Date";
        var typeHeader = System.Windows.Application.Current.TryFindResource("LblType")?.ToString() ?? "Type";
        var detailsHeader = System.Windows.Application.Current.TryFindResource("LblDescription")?.ToString() ?? "Details";
        var amountHeader = System.Windows.Application.Current.TryFindResource("LblAmount")?.ToString() ?? "Amount";
        var balanceHeader = System.Windows.Application.Current.TryFindResource("LblBalance")?.ToString() ?? "Balance";

        var lines = new List<string>
        {
            (string)System.Windows.Application.Current.TryFindResource("LblStatementTitle") ?? "Supplier Statement",
            $"{supplierLabel}: {SelectedSupplier.Name}",
            $"{dateRangeLabel}: {FromDate:yyyy-MM-dd} - {ToDate:yyyy-MM-dd}",
            new string('-', 72),
            string.Format("{0,-12} {1,-18} {2,-22} {3,10} {4,10}", dateHeader, typeHeader, detailsHeader, amountHeader, balanceHeader),
            new string('-', 72)
        };

        decimal balance = 0m;
        foreach (var transaction in transactions)
        {
            var amount = transaction.Amount;
            balance += IsIncrease(transaction.Type) ? amount : -amount;

            var typeLabel = isArabic
                ? GetArabicTypeLabel(transaction.Type)
                : GetEnglishTypeLabel(transaction.Type);

            lines.Add(string.Format(
                "{0,-12} {1,-18} {2,-22} {3,10} {4,10}",
                transaction.Date.ToString("yyyy-MM-dd"),
                typeLabel,
                Truncate(transaction.Description, 22),
                amount.ToString("0.00"),
                balance.ToString("0.00")
            ));
        }

        lines.Add(new string('-', 72));
        lines.Add($"{finalBalanceLabel}: {balance:0.00}");

        StatementText = string.Join(Environment.NewLine, lines);
    }

    private static string GetArabicTypeLabel(TransactionType type)
    {
        return type switch
        {
            TransactionType.GoldGiven => "ذهب طالع",
            TransactionType.GoldReceived => "ذهب داخل",
            TransactionType.PaymentIssued => "دفعة خارجة",
            TransactionType.PaymentReceived => "دفعة داخلة",
            _ => string.Empty
        };
    }

    private static string GetEnglishTypeLabel(TransactionType type)
    {
        return type switch
        {
            TransactionType.GoldGiven => "Gold Given",
            TransactionType.GoldReceived => "Gold Received",
            TransactionType.PaymentIssued => "Payment Issued",
            TransactionType.PaymentReceived => "Payment Received",
            _ => string.Empty
        };
    }

    private static bool IsIncrease(TransactionType type)
    {
        return type == TransactionType.GoldGiven || type == TransactionType.PaymentReceived;
    }

    private static string Truncate(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        return value.Length <= length ? value : value.Substring(0, length - 3) + "...";
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
