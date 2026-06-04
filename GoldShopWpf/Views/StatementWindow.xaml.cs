using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GoldShopCore.Models;
using GoldShopWpf.Services;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class StatementWindow : Window
{
    private readonly int _supplierId;
    private readonly string _supplierName;
    private FlowDocument _document = new();

    public StatementWindow(int supplierId, string supplierName, DateTime? fromDate, DateTime? toDate)
    {
        InitializeComponent();
        DialogWindowLayout.Apply(this);
        _supplierId = supplierId;
        _supplierName = supplierName;

        FromDate.SelectedDate = fromDate ?? DateTime.Today;
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

    private void OnDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        GenerateStatement();
    }

    private void GenerateStatement()
    {
        var from = FromDate.SelectedDate ?? DateTime.Today;
        var to = ToDate.SelectedDate ?? DateTime.Today;
        if (from > to)
        {
            MessageBox.Show(this, UiText.L("MsgFromBeforeTo"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var transactions = Services.AppServices.TransactionService.GetTransactions(_supplierId, from, to);
        var adjustments = Services.AppServices.OpeningBalanceAdjustmentService.GetAdjustments(_supplierId, from, to);
        var discounts = Services.AppServices.DiscountService.GetDiscounts(_supplierId, from, to);
        var summary = Services.AppServices.TransactionService.GetSummary(_supplierId, from, to);
        var overallSummary = Services.AppServices.TransactionService.GetSummary(_supplierId, null, null);
        var rows = BuildRows(transactions, adjustments, discounts, ShowNotesInTableCheckBox.IsChecked == true);
        _document = BuildDocument(from, to, rows, summary, overallSummary);
        PreviewViewer.Document = _document;
    }

    private FlowDocument BuildDocument(
        DateTime from,
        DateTime to,
        IReadOnlyList<StatementPreviewRow> rows,
        TraderSummary summary,
        TraderSummary overallSummary)
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
        if (ShowNotesInTableCheckBox.IsChecked == true)
        {
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

            foreach (var row in rows)
            {
                txTable.RowGroups[0].Rows.Add(CreateDataRow(
                    row.Date.ToString("yyyy/MM/dd"),
                    row.Type,
                    FormatNumber(row.Weight, UiText.L("LblWeightUnit")),
                    "-",
                    FormatNumber(row.Manufacturing, string.Empty),
                    FormatNumber(row.Improvement, string.Empty),
                    string.IsNullOrWhiteSpace(row.Notes) ? string.Empty : row.Notes));
            }
        }
        else
        {
            AddColumns(txTable, 90, 130, 90, 90, 120, 120, 170);
            txTable.RowGroups.Add(new TableRowGroup());
            txTable.RowGroups[0].Rows.Add(CreateHeaderRow(
                UiText.L("LblDate"),
                UiText.L("LblType"),
                UiText.L("LblWeight"),
                UiText.L("LblKarat"),
                UiText.L("LblTotalManufacturing"),
                UiText.L("LblTotalImprovement"),
                UiText.L("LblItem")));

            foreach (var row in rows)
            {
                txTable.RowGroups[0].Rows.Add(CreateDataRow(
                    row.Date.ToString("yyyy/MM/dd"),
                    row.Type,
                    FormatNumber(row.Weight, UiText.L("LblWeightUnit")),
                    "-",
                    FormatNumber(row.Manufacturing, string.Empty),
                    FormatNumber(row.Improvement, string.Empty),
                    string.IsNullOrWhiteSpace(row.Item) ? string.Empty : row.Item));
            }
        }

        doc.Blocks.Add(txTable);

        doc.Blocks.Add(new Paragraph(new Run($"ملخص الفترة: من {from:yyyy/MM/dd} إلى {to:yyyy/MM/dd}"))
        {
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 20, 0, 8),
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
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblTotalManufacturing"), FormatNumber(summary.FinalManufacturing, string.Empty)));
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblTotalImprovement"), FormatNumber(summary.FinalImprovement, string.Empty)));
        summaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblNetTotalReport"), FormatNumber(summary.FinalManufacturing + summary.FinalImprovement, string.Empty)));
        doc.Blocks.Add(summaryTable);

        doc.Blocks.Add(new Paragraph(new Run("الملخص العام"))
        {
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 20, 0, 8),
            TextAlignment = TextAlignment.Center
        });

        var overallSummaryTable = new Table
        {
            CellSpacing = 0
        };
        AddColumns(overallSummaryTable, 250, 180);
        overallSummaryTable.RowGroups.Add(new TableRowGroup());
        overallSummaryTable.RowGroups[0].Rows.Add(CreateHeaderRow(UiText.L("LblDescription"), UiText.L("LblAmount")));
        overallSummaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblTotalGold21"), FormatNumber(overallSummary.TotalGold21, UiText.L("LblWeightUnit"))));
        overallSummaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblTotalManufacturing"), FormatNumber(overallSummary.FinalManufacturing, string.Empty)));
        overallSummaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblTotalImprovement"), FormatNumber(overallSummary.FinalImprovement, string.Empty)));
        overallSummaryTable.RowGroups[0].Rows.Add(CreateDataRow(UiText.L("LblNetTotalReport"), FormatNumber(overallSummary.FinalManufacturing + overallSummary.FinalImprovement, string.Empty)));
        doc.Blocks.Add(overallSummaryTable);

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

    private static string GetDisplayItemName(string? itemName)
        => string.IsNullOrWhiteSpace(itemName) ? UiText.L("LblUnspecifiedItem") : itemName.Trim();

    private static string GetDisplayNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? UiText.L("LblNoNotesValue") : notes.Trim();

    private static IReadOnlyList<StatementPreviewRow> BuildRows(
        IReadOnlyList<SupplierTransaction> transactions,
        IReadOnlyList<OpeningBalanceAdjustment> adjustments,
        IReadOnlyList<DiscountRecord> discounts,
        bool showNotesInTable)
    {
        return transactions.Select(transaction => new StatementPreviewRow
            {
                SortId = transaction.Id,
                Date = transaction.Date,
                Type = transaction.Category switch
                {
                    TransactionCategories.GoldOutbound => UiText.L("LblGoldOutboundReport"),
                    TransactionCategories.GoldReceipt => UiText.L("LblGoldReceiptReport"),
                    TransactionCategories.FinishedGoldReceipt => UiText.L("LblFinishedGoldReceiptReport"),
                    TransactionCategories.CashPayment => UiText.L("LblCashPaymentReport"),
                    _ => transaction.Type.ToString()
                },
                Weight = transaction.Equivalent21,
                Item = GetDisplayItemName(transaction.ItemName),
                Notes = GetDisplayNotes(transaction.Notes),
                Manufacturing = transaction.TotalManufacturing,
                Improvement = transaction.TotalImprovement
            })
            .Concat(adjustments.Select(adjustment => new StatementPreviewRow
            {
                SortId = adjustment.Id,
                Date = adjustment.AdjustmentDate,
                Type = adjustment.Type == OpeningBalanceAdjustmentType.Manufacturing
                    ? UiText.L("LblOpeningBalanceManufacturingAdjustment")
                    : UiText.L("LblOpeningBalanceImprovementAdjustment"),
                Weight = 0m,
                Item = adjustment.Type == OpeningBalanceAdjustmentType.Manufacturing
                    ? UiText.L("LblOpeningBalanceManufacturingAdjustment")
                    : UiText.L("LblOpeningBalanceImprovementAdjustment"),
                Notes = GetDisplayNotes(adjustment.Notes),
                Manufacturing = adjustment.Type == OpeningBalanceAdjustmentType.Manufacturing ? adjustment.Amount : 0m,
                Improvement = adjustment.Type == OpeningBalanceAdjustmentType.Improvement ? adjustment.Amount : 0m
            }))
            .Concat(discounts.Select(discount => new StatementPreviewRow
            {
                SortId = discount.Id,
                Date = discount.CreatedAt.Date,
                Type = discount.Type == DiscountType.Manufacturing
                    ? UiText.L("LblManufacturingDiscountEntry")
                    : UiText.L("LblImprovementDiscountEntry"),
                Weight = 0m,
                Item = string.Empty,
                Notes = GetDisplayNotes(discount.Notes),
                Manufacturing = discount.Type == DiscountType.Manufacturing ? -discount.Amount : 0m,
                Improvement = discount.Type == DiscountType.Improvement ? -discount.Amount : 0m
            }))
            .OrderByDescending(row => row.Date)
            .ThenByDescending(row => row.SortId)
            .ToList();
    }

    private void OnShowNotesChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        GenerateStatement();
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
