using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

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
        var transactions = Services.AppServices.TransactionService.GetTransactions(_supplierId, from, to);
        var summary = Services.AppServices.TransactionService.GetSummary(_supplierId, from, to);

        var lines = new List<string>
        {
            "Trader Statement",
            $"Trader: {_supplierName}",
            $"Date Range: {from:yyyy-MM-dd} - {to:yyyy-MM-dd}",
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
        lines.Add($"Final Manufacturing: {FormatNumber(summary.FinalManufacturing, string.Empty)}");
        lines.Add($"Final Improvement: {FormatNumber(summary.FinalImprovement, string.Empty)}");

        _statement = string.Join(Environment.NewLine, lines);
        StatementText.Text = _statement;
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

        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Trader Statement");
    }
}
