using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GoldShopWpf.ViewModels;
using GoldShopWpf.Services;

namespace GoldShopWpf.Views;

public partial class WeeklyReportView : UserControl
{
    public WeeklyReportView()
    {
        InitializeComponent();
    }

    private void OnPrint(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WeeklyReportViewModel vm)
        {
            return;
        }

        var title = (string?)Application.Current.TryFindResource("NavWeeklyReport") ?? "Weekly Report";
        var supplierLabel = (string?)Application.Current.TryFindResource("LblTrader") ?? "Trader";
        var goldLabel = (string?)Application.Current.TryFindResource("LblTotalGold21") ?? "Total Gold (21K)";
        var manufacturingLabel = (string?)Application.Current.TryFindResource("LblTotalManufacturing") ?? "Manufacturing";
        var improvementLabel = (string?)Application.Current.TryFindResource("LblTotalImprovement") ?? "Improvement";

        var lines = new List<string>
        {
            title,
            $"{vm.FromDate:yyyy/MM/dd} - {vm.ToDate:yyyy/MM/dd}",
            new string('-', 88),
            string.Format("{0,-20} {1,14} {2,14} {3,14}", supplierLabel, goldLabel, manufacturingLabel, improvementLabel),
            new string('-', 88)
        };

        foreach (var r in vm.Reports)
        {
            lines.Add(string.Format(
                "{0,-20} {1,14} {2,14} {3,14}",
                r.SupplierName,
                FormatNumber(r.TotalGold21, "g"),
                FormatNumber(r.TotalManufacturing, string.Empty),
                FormatNumber(r.TotalImprovement, string.Empty)));
        }

        var content = string.Join(Environment.NewLine, lines);
        var font = (FontFamily)(Application.Current.TryFindResource("AppFontFamily") ?? new FontFamily("Tahoma"));

        var doc = new FlowDocument(new Paragraph(new Run(content)))
        {
            FontFamily = font,
            FontSize = 12,
            PagePadding = new Thickness(40)
        };

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, title);
        }
    }

    private static string FormatNumber(decimal value, string suffix)
        => string.IsNullOrWhiteSpace(suffix) ? $"{value:0.00}" : $"{value:0.00} {suffix}";
}
