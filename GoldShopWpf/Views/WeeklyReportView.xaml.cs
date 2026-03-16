using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GoldShopWpf.ViewModels;

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
        var supplierLabel = (string?)Application.Current.TryFindResource("LblSupplier") ?? "Supplier";
        var goldLabel = (string?)Application.Current.TryFindResource("LblGoldGiven") ?? "Gold";
        var paymentsLabel = (string?)Application.Current.TryFindResource("LblTotalPayments") ?? "Payments";
        var balanceLabel = (string?)Application.Current.TryFindResource("LblCurrentBalance") ?? "Balance";

        var lines = new List<string>
        {
            title,
            $"{vm.FromDate:yyyy-MM-dd} - {vm.ToDate:yyyy-MM-dd}",
            new string('-', 72),
            string.Format("{0,-24} {1,12} {2,12} {3,12}", supplierLabel, goldLabel, paymentsLabel, balanceLabel),
            new string('-', 72)
        };

        foreach (var r in vm.Reports)
        {
            lines.Add(string.Format("{0,-24} {1,12:0.00} {2,12:0.00} {3,12:0.00}", r.SupplierName, r.TotalGold, r.TotalPayments, r.CurrentBalance));
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
}
