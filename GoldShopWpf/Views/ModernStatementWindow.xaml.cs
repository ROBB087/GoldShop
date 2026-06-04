using System.IO;
using System.ComponentModel;
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
    private enum ReceiptShareFormat
    {
        Image,
        Pdf
    }

    private readonly ModernStatementPreviewViewModel _viewModel;
    private FlowDocument _printDocument = new();

    public ModernStatementWindow(int supplierId, string supplierName, DateTime? fromDate, DateTime? toDate, bool showNotesInTable = false)
    {
        InitializeComponent();
        DialogWindowLayout.Apply(this);
        _viewModel = new ModernStatementPreviewViewModel(supplierId, supplierName, fromDate, toDate, showNotesInTable);
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateNotesColumnVisibility();
        RefreshPrintDocument();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Detach();
        base.OnClosed(e);
    }

    private void RefreshPrintDocument()
    {
        _viewModel.Load();
        UpdateNotesColumnVisibility();
        _printDocument = BuildDocument(_viewModel.GetRows(), _viewModel.GetSummary());
    }

    private void OnDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        UpdateNotesColumnVisibility();
        _printDocument = BuildDocument(_viewModel.GetRows(), _viewModel.GetSummary());
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ModernStatementPreviewViewModel.ShowNotesInTable))
        {
            Dispatcher.Invoke(() =>
            {
                UpdateNotesColumnVisibility();
                RefreshPrintDocument();
            });
        }
    }

    private void UpdateNotesColumnVisibility()
    {
        if (NotesColumn == null)
        {
            return;
        }

        NotesColumn.Visibility = _viewModel.ShowNotesInTable
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private FlowDocument BuildDocument(
        IReadOnlyList<StatementPreviewRow> rows,
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
        var widths = _viewModel.ShowNotesInTable
            ? new[] { 90d, 130d, 120d, 120d, 100d, 100d, 150d }
            : new[] { 90d, 130d, 120d, 120d, 100d, 100d };
        foreach (var width in widths)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        }

        table.RowGroups.Add(new TableRowGroup());
        table.RowGroups[0].Rows.Add(_viewModel.ShowNotesInTable
            ? CreateHeaderRow(
                UiText.L("LblDate"),
                UiText.L("LblType"),
                UiText.L("LblEquivalent21"),
                UiText.L("LblItem"),
                UiText.L("LblTotalManufacturing"),
                UiText.L("LblTotalImprovement"),
                UiText.L("LblNotes"))
            : CreateHeaderRow(
                UiText.L("LblDate"),
                UiText.L("LblType"),
                UiText.L("LblEquivalent21"),
                UiText.L("LblItem"),
                UiText.L("LblTotalManufacturing"),
                UiText.L("LblTotalImprovement")));

        foreach (var row in rows)
        {
            table.RowGroups[0].Rows.Add(_viewModel.ShowNotesInTable
                ? CreateRow(
                    row.Date.ToString("yyyy/MM/dd"),
                    row.Type,
                    $"{row.Weight:0.####}",
                    string.IsNullOrWhiteSpace(row.Item) ? "-" : row.Item,
                    $"{row.Manufacturing:0.##}",
                    $"{row.Improvement:0.##}",
                    string.IsNullOrWhiteSpace(row.Notes) ? "-" : row.Notes)
                : CreateRow(
                    row.Date.ToString("yyyy/MM/dd"),
                    row.Type,
                    $"{row.Weight:0.####}",
                    string.IsNullOrWhiteSpace(row.Item) ? "-" : row.Item,
                    $"{row.Manufacturing:0.##}",
                    $"{row.Improvement:0.##}"));
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
        doc.Blocks.Add(new Paragraph(new Run(_viewModel.SummaryPeriodTitle)) { FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 20, 0, 8) });
        doc.Blocks.Add(totals);

        var overallTotals = new Table { CellSpacing = 0 };
        overallTotals.Columns.Add(new TableColumn { Width = new GridLength(260) });
        overallTotals.Columns.Add(new TableColumn { Width = new GridLength(180) });
        overallTotals.RowGroups.Add(new TableRowGroup());
        overallTotals.RowGroups[0].Rows.Add(CreateHeaderRow(UiText.L("LblDescription"), UiText.L("LblAmount")));
        overallTotals.RowGroups[0].Rows.Add(CreateRow(UiText.L("LblTotalGoldReport"), _viewModel.OverallTotalGoldDisplay));
        overallTotals.RowGroups[0].Rows.Add(CreateRow(UiText.L("LblTotalManufacturingReport"), _viewModel.OverallTotalManufacturingDisplay));
        overallTotals.RowGroups[0].Rows.Add(CreateRow(UiText.L("LblTotalImprovement"), _viewModel.OverallTotalImprovementDisplay));
        overallTotals.RowGroups[0].Rows.Add(CreateRow(UiText.L("LblNetTotalReport"), _viewModel.OverallNetTotalDisplay));
        doc.Blocks.Add(new Paragraph(new Run(_viewModel.OverallSummaryTitle)) { FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 20, 0, 8) });
        doc.Blocks.Add(overallTotals);

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

    private void OnExportImage(object sender, RoutedEventArgs e)
    {
        var receiptFolder = ReportExportService.GetReceiptFolder(_viewModel.SupplierName);
        Directory.CreateDirectory(receiptFolder);
        var savedAt = DateTime.Now;
        var path = ReportExportService.ExportVisualAsPng(
            CaptureSurface,
            Path.GetFileNameWithoutExtension(ReportExportService.GetReceiptFileName(_viewModel.SupplierName, savedAt, "png")),
            receiptFolder);
        if (!string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(UiText.Format("MsgReportSaved", path), UiText.L("TitleExportImage"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnExportPdf(object sender, RoutedEventArgs e)
    {
        var receiptFolder = ReportExportService.GetReceiptFolder(_viewModel.SupplierName);
        Directory.CreateDirectory(receiptFolder);
        var savedAt = DateTime.Now;
        var dialog = new SaveFileDialog
        {
            Title = UiText.L("TitleExportPdf"),
            Filter = UiText.L("FilterPdf"),
            FileName = ReportExportService.GetReceiptFileName(_viewModel.SupplierName, savedAt, "pdf"),
            InitialDirectory = receiptFolder
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        QuestPDF.Settings.License = LicenseType.Community;
        var isRtl = (FlowDirection)(Application.Current.TryFindResource("AppFlowDirection") ?? FlowDirection.RightToLeft) == FlowDirection.RightToLeft;
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(style => isRtl
                    ? style.FontSize(11).DirectionFromRightToLeft()
                    : style.FontSize(11).DirectionFromLeftToRight());

                if (isRtl)
                {
                    page.ContentFromRightToLeft();
                }

                page.Header().Column(column =>
                {
                    column.Item().Text(_viewModel.ReportTitle).AlignRight().FontSize(22).Bold().FontColor(Colors.Amber.Darken2);
                    column.Item().Text(_viewModel.SupplierName).AlignRight();
                });

                page.Content().PaddingVertical(18).Column(column =>
                {
                    column.Spacing(16);
                    column.Item().Text($"{UiText.L("ReceiptDateRange")}: {_viewModel.FromDate:yyyy/MM/dd} - {_viewModel.ToDate:yyyy/MM/dd}").AlignRight();

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
                            if (_viewModel.ShowNotesInTable)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblDate")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblType")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblEquivalent21")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblItem")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblTotalManufacturing")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblTotalImprovement")).AlignRight().SemiBold();
                            if (_viewModel.ShowNotesInTable)
                            {
                                header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblNotes")).AlignRight().SemiBold();
                            }
                        });

                        foreach (var row in _viewModel.Rows)
                        {
                            table.Cell().Padding(8).Text(row.Date.ToString("yyyy/MM/dd")).AlignRight();
                            table.Cell().Padding(8).Text(row.Type).AlignRight();
                            table.Cell().Padding(8).Text(row.Weight.ToString("0.####")).AlignRight();
                            table.Cell().Padding(8).Text(string.IsNullOrWhiteSpace(row.Item) ? "-" : row.Item).AlignRight();
                            table.Cell().Padding(8).Text(row.Manufacturing.ToString("0.##")).AlignRight();
                            table.Cell().Padding(8).Text(row.Improvement.ToString("0.##")).AlignRight();
                            if (_viewModel.ShowNotesInTable)
                            {
                                table.Cell().Padding(8).Text(string.IsNullOrWhiteSpace(row.Notes) ? "-" : row.Notes).AlignRight();
                            }
                        }
                    });

                    column.Item().PaddingTop(12).Text(_viewModel.SummaryPeriodTitle).AlignRight().FontSize(12).SemiBold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                        });

                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblDescription")).AlignRight().SemiBold();
                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblAmount")).AlignRight().SemiBold();

                        table.Cell().Padding(8).Text(UiText.L("LblTotalGoldReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.TotalGoldDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblTotalManufacturingReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.TotalManufacturingDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblTotalImprovement")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.TotalImprovementDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblNetTotalReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.NetTotalDisplay).AlignRight();
                    });

                    column.Item().PaddingTop(12).Text(_viewModel.OverallSummaryTitle).AlignRight().FontSize(12).SemiBold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                        });

                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblDescription")).AlignRight().SemiBold();
                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblAmount")).AlignRight().SemiBold();

                        table.Cell().Padding(8).Text(UiText.L("LblTotalGoldReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.OverallTotalGoldDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblTotalManufacturingReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.OverallTotalManufacturingDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblTotalImprovement")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.OverallTotalImprovementDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblNetTotalReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.OverallNetTotalDisplay).AlignRight();
                    });
                });
            });
        }).GeneratePdf(dialog.FileName);

        MessageBox.Show(UiText.Format("MsgReportSaved", dialog.FileName), UiText.L("TitleExportPdf"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string ExportReceiptAsWhatsAppPdf()
    {
        var folder = ReportExportService.GetReceiptFolder(_viewModel.SupplierName);
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(
            folder,
            ReportExportService.GetReceiptFileName(_viewModel.SupplierName, DateTime.Now, "pdf"));

        QuestPDF.Settings.License = LicenseType.Community;
        var isRtl = (FlowDirection)(Application.Current.TryFindResource("AppFlowDirection") ?? FlowDirection.RightToLeft) == FlowDirection.RightToLeft;
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(style => isRtl
                    ? style.FontSize(11).DirectionFromRightToLeft()
                    : style.FontSize(11).DirectionFromLeftToRight());

                if (isRtl)
                {
                    page.ContentFromRightToLeft();
                }

                page.Header().Column(column =>
                {
                    column.Item().Text(_viewModel.ReportTitle).AlignRight().FontSize(22).Bold().FontColor(Colors.Amber.Darken2);
                    column.Item().Text(_viewModel.SupplierName).AlignRight();
                });

                page.Content().PaddingVertical(18).Column(column =>
                {
                    column.Spacing(16);
                    column.Item().Text($"{UiText.L("ReceiptDateRange")}: {_viewModel.FromDate:yyyy/MM/dd} - {_viewModel.ToDate:yyyy/MM/dd}").AlignRight();

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
                            if (_viewModel.ShowNotesInTable)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblDate")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblType")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblEquivalent21")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblItem")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblTotalManufacturing")).AlignRight().SemiBold();
                            header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblTotalImprovement")).AlignRight().SemiBold();
                            if (_viewModel.ShowNotesInTable)
                            {
                                header.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblNotes")).AlignRight().SemiBold();
                            }
                        });

                        foreach (var row in _viewModel.Rows)
                        {
                            table.Cell().Padding(8).Text(row.Date.ToString("yyyy/MM/dd")).AlignRight();
                            table.Cell().Padding(8).Text(row.Type).AlignRight();
                            table.Cell().Padding(8).Text(row.Weight.ToString("0.####")).AlignRight();
                            table.Cell().Padding(8).Text(string.IsNullOrWhiteSpace(row.Item) ? "-" : row.Item).AlignRight();
                            table.Cell().Padding(8).Text(row.Manufacturing.ToString("0.##")).AlignRight();
                            table.Cell().Padding(8).Text(row.Improvement.ToString("0.##")).AlignRight();
                            if (_viewModel.ShowNotesInTable)
                            {
                                table.Cell().Padding(8).Text(string.IsNullOrWhiteSpace(row.Notes) ? "-" : row.Notes).AlignRight();
                            }
                        }
                    });

                    column.Item().PaddingTop(12).Text(_viewModel.SummaryPeriodTitle).AlignRight().FontSize(12).SemiBold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                        });

                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblDescription")).AlignRight().SemiBold();
                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblAmount")).AlignRight().SemiBold();

                        table.Cell().Padding(8).Text(UiText.L("LblTotalGoldReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.TotalGoldDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblTotalManufacturingReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.TotalManufacturingDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblTotalImprovement")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.TotalImprovementDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblNetTotalReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.NetTotalDisplay).AlignRight();
                    });

                    column.Item().PaddingTop(12).Text(_viewModel.OverallSummaryTitle).AlignRight().FontSize(12).SemiBold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                        });

                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblDescription")).AlignRight().SemiBold();
                        table.Cell().Padding(8).Background("#F5F6FA").Text(UiText.L("LblAmount")).AlignRight().SemiBold();

                        table.Cell().Padding(8).Text(UiText.L("LblTotalGoldReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.OverallTotalGoldDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblTotalManufacturingReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.OverallTotalManufacturingDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblTotalImprovement")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.OverallTotalImprovementDisplay).AlignRight();
                        table.Cell().Padding(8).Text(UiText.L("LblNetTotalReport")).AlignRight();
                        table.Cell().Padding(8).Text(_viewModel.OverallNetTotalDisplay).AlignRight();
                    });
                });
            });
        }).GeneratePdf(filePath);

        ReportExportService.OpenReceiptFolder(_viewModel.SupplierName);
        return filePath;
    }

    private ReceiptShareFormat? ShowReceiptShareFormatDialog()
    {
        ReceiptShareFormat? selectedFormat = null;
        var dialog = new Window
        {
            Title = UiText.L("BtnShare"),
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            FlowDirection = FlowDirection.RightToLeft,
            Background = System.Windows.Media.Brushes.White,
            Style = null,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                MinWidth = 360,
                Children =
                {
                    new TextBlock
                    {
                        Text = "\u0627\u062e\u062a\u0631 \u0637\u0631\u064a\u0642\u0629 \u062d\u0641\u0638 \u0627\u0644\u0643\u0634\u0641 \u0642\u0628\u0644 \u0627\u0644\u0645\u0634\u0627\u0631\u0643\u0629 \u0639\u0628\u0631 WhatsApp",
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    },
                    new TextBlock
                    {
                        Text = "\u0645\u0644\u062d\u0648\u0638\u0629: \u0625\u0630\u0627 \u0643\u0627\u0646 \u0627\u0644\u0643\u0634\u0641 \u0643\u0628\u064a\u0631\u0627\u064b \u064a\u064f\u0641\u0636\u0644 \u0627\u062e\u062a\u064a\u0627\u0631 \u0645\u0644\u0641 PDF.",
                        Foreground = System.Windows.Media.Brushes.DimGray,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 18)
                    }
                }
            }
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left
        };

        var imageButton = new Button { Content = "\u0635\u0648\u0631\u0647", MinWidth = 92, Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
        var pdfButton = new Button { Content = "\u0645\u0644\u0641 PDF", MinWidth = 92, Margin = new Thickness(8, 0, 0, 0) };
        var cancelButton = new Button { Content = "\u0625\u0644\u063a\u0627\u0621", MinWidth = 92, IsCancel = true };

        imageButton.Click += (_, _) =>
        {
            selectedFormat = ReceiptShareFormat.Image;
            dialog.DialogResult = true;
        };
        pdfButton.Click += (_, _) =>
        {
            selectedFormat = ReceiptShareFormat.Pdf;
            dialog.DialogResult = true;
        };

        actions.Children.Add(imageButton);
        actions.Children.Add(pdfButton);
        actions.Children.Add(cancelButton);
        ((StackPanel)dialog.Content).Children.Add(actions);

        return dialog.ShowDialog() == true ? selectedFormat : null;
    }

    private void OnShare(object sender, RoutedEventArgs e)
    {
        var choice = ShowReceiptShareFormatDialog();

        if (choice == null)
        {
            return;
        }

        var path = choice == ReceiptShareFormat.Image
            ? ReportExportService.ExportReceiptAsWhatsAppImage(CaptureSurface, _viewModel.SupplierName)
            : ExportReceiptAsWhatsAppPdf();

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
