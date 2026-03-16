using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.Views;

public partial class StatementWindow : Window
{
    private readonly int _supplierId;
    private readonly string _supplierName;
    private string _statement = string.Empty;

    public StatementWindow(int supplierId, string supplierName, DateTime? fromDate, DateTime? toDate)
    {
        InitializeComponent();
        _supplierId = supplierId;
        _supplierName = supplierName;

        FromDate.SelectedDate = fromDate ?? DateTime.Today.AddMonths(-1);
        ToDate.SelectedDate = toDate ?? DateTime.Today;

        LoadLogo();
        GenerateStatement();
    }

    private void LoadLogo()
    {
        var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
        if (System.IO.File.Exists(logoPath))
        {
            LogoImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(logoPath));
            LogoImage.Visibility = Visibility.Visible;
        }
    }

    private void OnGenerate(object sender, RoutedEventArgs e)
    {
        GenerateStatement();
    }

    private void GenerateStatement()
    {
        var from = FromDate.SelectedDate ?? DateTime.Today.AddMonths(-1);
        var to = ToDate.SelectedDate ?? DateTime.Today;
        var transactions = AppServices.TransactionService.GetTransactions(_supplierId, from, to);

        var isArabic = GoldShopWpf.Services.LocalizationService.CurrentLanguage == "ar";
        var supplierLabel = isArabic ? "المورد" : "Supplier";
        var dateRangeLabel = isArabic ? "الفترة" : "Date Range";
        var finalBalanceLabel = isArabic ? "الرصيد النهائي" : "Final Balance";
        var dateHeader = (string)Application.Current.TryFindResource("LblDate") ?? "Date";
        var typeHeader = (string)Application.Current.TryFindResource("LblType") ?? "Type";
        var detailsHeader = (string)Application.Current.TryFindResource("LblDescription") ?? "Details";
        var amountHeader = (string)Application.Current.TryFindResource("LblAmount") ?? "Amount";
        var balanceHeader = (string)Application.Current.TryFindResource("LblBalance") ?? "Balance";

        var lines = new List<string>
        {
            (string)Application.Current.TryFindResource("LblStatementTitle") ?? "Supplier Statement",
            $"{supplierLabel}: {_supplierName}",
            $"{dateRangeLabel}: {from:yyyy-MM-dd} - {to:yyyy-MM-dd}",
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

        _statement = string.Join(Environment.NewLine, lines);
        StatementText.Text = _statement;
    }

    private static string Truncate(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        return value.Length <= length ? value : value.Substring(0, length - 3) + "...";
    }

    private void OnPrint(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_statement))
        {
            GenerateStatement();
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var font = (FontFamily)(Application.Current.TryFindResource("AppFontFamily") ?? new FontFamily("Tahoma"));
        var document = new FlowDocument(new Paragraph(new Run(_statement)))
        {
            FontFamily = font,
            FontSize = 12,
            PagePadding = new Thickness(40)
        };

        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Supplier Statement");
    }

    private static bool IsIncrease(TransactionType type)
    {
        return type == TransactionType.GoldGiven || type == TransactionType.PaymentReceived;
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
}
