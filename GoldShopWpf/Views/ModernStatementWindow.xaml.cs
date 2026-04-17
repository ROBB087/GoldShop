using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using GoldShopCore.Models;
using GoldShopWpf.Services;
using GoldShopWpf.ViewModels;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GoldShopWpf.Views;

public partial class ModernStatementWindow : Window
{
    private readonly ModernStatementPreviewViewModel _viewModel;
    private FlowDocument _printDocument = new();

    public ModernStatementWindow(int supplierId, string supplierName, DateTime? fromDate, DateTime? toDate)
    {
        InitializeComponent();
        DialogWindowLayout.Apply(this);
        _viewModel = new ModernStatementPreviewViewModel(supplierId, supplierName, fromDate, toDate);
        DataContext = _viewModel;
        RefreshPrintDocument();
    }

    private void RefreshPrintDocument()
    {
        _viewModel.Load();
        _printDocument = BuildDocument(_viewModel.GetTransactions(), _viewModel.GetSummary());
    }

    private void OnDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        _printDocument = BuildDocument(_viewModel.GetTransactions(), _viewModel.GetSummary());
    }

    private FlowDocument BuildDocument(
        IReadOnlyList<SupplierTransaction> transactions,
        TraderSummary summary)
    {
        var font = (System.Windows.Media.FontFamily)(Application.Current.TryFindResource("AppFontFamily") ?? new System.Windows.Media.FontFamily("Segoe UI"));
        var doc = new FlowDocument
        {
            FontFamily = font,
            FontSize = 13,
            PagePadding = new Thickness(36),
            TextAlignment = TextAlignment.Center,
            ColumnWidth = double.PositiveInfinity,
            FlowDirection = (FlowDirection)(Application.Current.TryFindResource("AppFlowDirection") ?? FlowDirection.RightToLeft)
        };

        doc.Blocks.Add(new Paragraph(new Run(_viewModel.ReportTitle))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8),
            TextAlignment = TextAlignment.Center
        });

        doc.Blocks.Add(new Paragraph(new Run(_viewModel.SupplierName))
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4),
            TextAlignment = TextAlignment.Center
        });

        doc.Blocks.Add(new Paragraph(new Run($"{UiText.L("ReceiptDateRange")}: {_viewModel.FromDate:yyyy/MM/dd} - {_viewModel.ToDate:yyyy/MM/dd}"))
        {
            Margin = new Thickness(0, 0, 0, 18),
            TextAlignment = TextAlignment.Center
        });

        var table = new Table { CellSpacing = 0 };
        foreach (var width in new[] { 90d, 130d, 120d, 120d, 100d, 100d })
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        }

        table.RowGroups.Add(new TableRowGroup());
        table.RowGroups[0].Rows.Add(CreateHeaderRow(
            UiText.L("LblDate"),
            UiText.L("LblType"),
            UiText.L("LblEquivalent21"),
            UiText.L("LblItem"),
            UiText.L("LblTotalManufacturing"),
            UiText.L("LblTotalImprovement")));

        foreach (var transaction in transactions)
        {
            table.RowGroups[0].Rows.Add(CreateRow(
                transaction.Date.ToString("yyyy/MM/dd"),
                FormatType(transaction),
                $"{transaction.Equivalent21:0.####}",
                string.IsNullOrWhiteSpace(transaction.ItemName) ? "-" : transaction.ItemName,
                $"{transaction.TotalManufacturing:0.##}",
                $"{transaction.TotalImprovement:0.##}"));
        }

        doc.Blocks.Add(table);

        var totals = new Table { CellSpacing = 0 };
        totals.Columns.Add(new TableColumn { Width = new GridLength(260) });
        totals.Columns.Add(new TableColumn { Width = new GridLength(180) });
        totals.RowGroups.Add(new TableRowGroup());
        totals.RowGroups[0].Rows.Add(CreateHeaderRow(UiText.L("LblDescription"), UiText.L("LblAmount")));
        totals.RowGroups[0].Rows.Add(CreateRow(UiText.L("LblTotalGoldReport"), _viewModel.TotalGoldDisplay));
        totals.RowGroups[0].Rows.Add(CreateRow(UiText.L("LblTotalManufacturingReport"), _viewModel.TotalManufacturingDisplay));
        totals.RowGroups[0].Rows.Add(CreateRow(UiText.L("LblTotalImprovement"), _viewModel.TotalImprovementDisplay));
        totals.RowGroups[0].Rows.Add(CreateRow(UiText.L("LblNetTotalReport"), _viewModel.NetTotalDisplay));
        doc.Blocks.Add(new Paragraph(new Run(UiText.L("LblReportTotals"))) { FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 20, 0, 10) });
        doc.Blocks.Add(totals);

        return doc;
    }

    private static TableRow CreateHeaderRow(params string[] values)
    {
        var row = new TableRow { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 246, 250)) };
        foreach (var value in values)
        {
            row.Cells.Add(CreateCell(value, true));
        }

        return row;
    }

    private static TableRow CreateRow(params string[] values)
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
        return new TableCell(new Paragraph(new Run(value)) { Margin = new Thickness(0), TextAlignment = TextAlignment.Center })
        {
            Padding = new Thickness(8),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(0.5),
            FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal
        };
    }

    private static string FormatType(SupplierTransaction transaction)
    {
        return transaction.Category switch
        {
            TransactionCategories.GoldOutbound => UiText.L("LblGoldOutboundReport"),
            TransactionCategories.GoldReceipt => UiText.L("LblGoldReceiptReport"),
            TransactionCategories.FinishedGoldReceipt => UiText.L("LblFinishedGoldReceiptReport"),
            TransactionCategories.CashPayment => UiText.L("LblCashPaymentReport"),
            _ => transaction.Type.ToString()
        };
    }

    private void OnExportImage(object sender, RoutedEventArgs e)
    {
        var path = ReportExportService.ExportVisualAsPng(CaptureSurface, $"statement-{DateTime.Now:yyyyMMdd-HHmm}");
        if (!string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(UiText.Format("MsgReportSaved", path), UiText.L("TitleExportImage"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnExportPdf(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = UiText.L("TitleExportPdf"),
            Filter = UiText.L("FilterPdf"),
            FileName = $"statement-{DateTime.Now:yyyyMMdd-HHmm}.pdf"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(style => style.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text(_viewModel.ReportTitle).FontSize(22).Bold().FontColor(Colors.Amber.Darken2);
                    column.Item().Text(_viewModel.SupplierName);
                });

                page.Content().PaddingVertical(18).Column(column =>
                {
                    column.Spacing(16);
                    column.Item().Text($"{UiText.L("ReceiptDateRange")}: {_viewModel.FromDate:yyyy/MM/dd} - {_viewModel.ToDate:yyyy/MM/dd}");

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblDate")).SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblType")).SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblEquivalent21")).SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblItem")).SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblTotalManufacturing")).SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblTotalImprovement")).SemiBold();
                        });

                        foreach (var row in _viewModel.Rows)
                        {
                            table.Cell().Padding(8).Text(row.Date.ToString("yyyy/MM/dd"));
                            table.Cell().Padding(8).Text(row.Type);
                            table.Cell().Padding(8).Text(row.Weight.ToString("0.####"));
                            table.Cell().Padding(8).Text(string.IsNullOrWhiteSpace(row.Item) ? "-" : row.Item);
                            table.Cell().Padding(8).Text(row.Manufacturing.ToString("0.##"));
                            table.Cell().Padding(8).Text(row.Improvement.ToString("0.##"));
                        }
                    });

                    column.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                        });

                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblDescription")).SemiBold();
                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblAmount")).SemiBold();

                        table.Cell().Padding(8).Text(UiText.L("LblTotalGoldReport"));
                        table.Cell().Padding(8).Text(_viewModel.TotalGoldDisplay);
                        table.Cell().Padding(8).Text(UiText.L("LblTotalManufacturingReport"));
                        table.Cell().Padding(8).Text(_viewModel.TotalManufacturingDisplay);
                        table.Cell().Padding(8).Text(UiText.L("LblTotalImprovement"));
                        table.Cell().Padding(8).Text(_viewModel.TotalImprovementDisplay);
                        table.Cell().Padding(8).Text(UiText.L("LblNetTotalReport"));
                        table.Cell().Padding(8).Text(_viewModel.NetTotalDisplay);
                    });
                });
            });
        }).GeneratePdf(dialog.FileName);

        MessageBox.Show(UiText.Format("MsgReportSaved", dialog.FileName), UiText.L("TitleExportPdf"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnShare(object sender, RoutedEventArgs e)
    {
        var path = ReportExportService.ExportVisualAsShareImage(CaptureSurface, $"statement-share-{DateTime.Now:yyyyMMdd-HHmm}");
        ReportExportService.ShareFileViaWhatsApp(path, _viewModel.SupplierPhone);
    }

    private void OnPrint(object sender, RoutedEventArgs e)
    {
        RefreshPrintDocument();
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            dialog.PrintDocument(((IDocumentPaginatorSource)_printDocument).DocumentPaginator, UiText.L("ReceiptTitle"));
        }
    }
}
