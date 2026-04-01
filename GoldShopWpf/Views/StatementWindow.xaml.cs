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
    private FlowDocument _document = new();

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
        LogoImage.Source = new System.Windows.Media.Imaging.BitmapImage(
            new Uri("pack://application:,,,/Resources/logo.png", UriKind.Absolute));
        LogoImage.Visibility = Visibility.Visible;
    }

    private void OnGenerate(object sender, RoutedEventArgs e)
    {
        GenerateStatement();
    }

    private void GenerateStatement()
    {
        var from = FromDate.SelectedDate ?? DateTime.Today.AddMonths(-1);
        var to = ToDate.SelectedDate ?? DateTime.Today;
        if (from > to)
        {
            MessageBox.Show(this, UiText.L("MsgFromBeforeTo"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var transactions = Services.AppServices.TransactionService.GetTransactions(_supplierId, from, to);
        var summary = Services.AppServices.TransactionService.GetSummary(_supplierId, from, to);
        _document = BuildDocument(from, to, transactions, summary);
        PreviewViewer.Document = _document;
    }

    private FlowDocument BuildDocument(DateTime from, DateTime to, IReadOnlyList<SupplierTransaction> transactions, TraderSummary summary)
    {
        var isArabic = LocalizationService.CurrentLanguage == "ar";
        var font = (FontFamily)(Application.Current.TryFindResource("AppFontFamily") ?? new FontFamily("Tahoma"));
        var doc = new FlowDocument
        {
            FontFamily = font,
            FontSize = 13,
            PagePadding = new Thickness(36),
            TextAlignment = TextAlignment.Center,
            FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            ColumnWidth = double.PositiveInfinity
        };

        doc.Blocks.Add(new Paragraph(new Run(UiText.L("ReceiptTitle")))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8),
            TextAlignment = TextAlignment.Center
        });

        doc.Blocks.Add(new Paragraph(new Run(_supplierName))
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4),
            TextAlignment = TextAlignment.Center
        });

        doc.Blocks.Add(new Paragraph(new Run($"{UiText.L("ReceiptDateRange")}: {from:yyyy/MM/dd} - {to:yyyy/MM/dd}"))
        {
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 18),
            TextAlignment = TextAlignment.Center
        });

        doc.Blocks.Add(new Paragraph(new Run(UiText.L("ReceiptTransactionTable")))
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10),
            TextAlignment = TextAlignment.Center
        });

        var txTable = new Table
        {
            CellSpacing = 0
        };
        AddColumns(txTable, 90, 130, 90, 90, 120, 120, 170);
        txTable.RowGroups.Add(new TableRowGroup());
        txTable.RowGroups[0].Rows.Add(CreateHeaderRow(
            UiText.L("LblDate"),
            UiText.L("LblType"),
            UiText.L("LblWeight"),
            UiText.L("LblKarat"),
            UiText.L("LblTotalManufacturing"),
            UiText.L("LblTotalImprovement"),
            UiText.L("LblNotes")));

        foreach (var transaction in transactions)
        {
            txTable.RowGroups[0].Rows.Add(CreateDataRow(
                transaction.Date.ToString("yyyy/MM/dd"),
                FormatType(transaction),
                FormatNumber(transaction.OriginalWeight, UiText.L("LblWeightUnit")),
                $"{transaction.OriginalKarat}",
                FormatNumber(transaction.TotalManufacturing, string.Empty),
                FormatNumber(transaction.TotalImprovement, string.Empty),
                transaction.Notes ?? string.Empty));
        }

        doc.Blocks.Add(txTable);

        doc.Blocks.Add(new Paragraph(new Run(UiText.L("ReceiptSummary")))
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 20, 0, 10),
            TextAlignment = TextAlignment.Center
        });

        var summaryTable = new Table
        {
            CellSpacing = 0
        };
        AddColumns(summaryTable, 250, 180);
        summaryTable.RowGroups.Add(new TableRowGroup());
        summaryTable.RowGroups[0].Rows.Add(CreateHeaderRow(UiText.L("LblDescription"), UiText.L("LblAmount")));
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblTotalGold21"), FormatNumber(summary.TotalGold21, UiText.L("LblWeightUnit"))));
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblTotalManufacturing"), FormatNumber(summary.TotalManufacturing, string.Empty)));
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblTotalImprovement"), FormatNumber(summary.TotalImprovement, string.Empty)));
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblManufacturingDiscounts"), FormatNumber(summary.ManufacturingDiscounts, string.Empty)));
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblImprovementDiscounts"), FormatNumber(summary.ImprovementDiscounts, string.Empty)));
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblFinalManufacturing"), FormatNumber(summary.FinalManufacturing, string.Empty)));
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblFinalImprovement"), FormatNumber(summary.FinalImprovement, string.Empty)));
        doc.Blocks.Add(summaryTable);

        return doc;
    }

    private static void AddColumns(Table table, params double[] widths)
    {
        foreach (var width in widths)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        }
    }

    private static TableRow CreateHeaderRow(params string[] values)
    {
        var row = new TableRow { Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)) };
        foreach (var value in values)
        {
            row.Cells.Add(CreateCell(value, true));
        }

        return row;
    }

    private static TableRow CreateDataRow(params string[] values)
    {
        var row = new TableRow();
        foreach (var value in values)
        {
            row.Cells.Add(CreateCell(value, false));
        }

        return row;
    }

    private static TableCell CreateCell(string value, bool isHeader)
    {
        return new TableCell(new Paragraph(new Run(value))
        {
            Margin = new Thickness(0),
            TextAlignment = TextAlignment.Center
        })
        {
            Padding = new Thickness(8),
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(0.5),
            TextAlignment = TextAlignment.Center,
            FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal
        };
    }

    private static string FormatNumber(decimal value, string suffix)
        => string.IsNullOrWhiteSpace(suffix) ? $"{value:0.00}" : $"{value:0.00} {suffix}";

    private static string FormatType(SupplierTransaction transaction)
    {
        return transaction.Category switch
        {
            TransactionCategories.GoldOutbound => LocalizationService.CurrentLanguage == "ar" ? "صرف ذهب" : "Gold Out",
            TransactionCategories.GoldReceipt => LocalizationService.CurrentLanguage == "ar" ? "استلام ذهب" : "Gold Receipt",
            TransactionCategories.CashPayment => LocalizationService.CurrentLanguage == "ar" ? "سداد نقدي" : "Cash Payment",
            _ => transaction.Type.ToString()
        };
    }

    private void OnPrint(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        dialog.PrintDocument(((IDocumentPaginatorSource)_document).DocumentPaginator, UiText.L("ReceiptTitle"));
    }
}
