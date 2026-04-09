using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GoldShopWpf.ViewModels;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GoldShopWpf.Services;

public static class ReportExportService
{
    public static string? ExportVisualAsPng(FrameworkElement element, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = UiText.L("TitleExportImage"),
            Filter = UiText.L("FilterPng"),
            FileName = $"{defaultFileName}.png"
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        SaveElementAsPng(element, dialog.FileName);
        return dialog.FileName;
    }

    public static string ExportVisualAsShareImage(FrameworkElement element, string defaultFileName)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GoldShop Reports");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, $"{defaultFileName}.png");
        SaveElementAsPng(element, filePath);
        return filePath;
    }

    public static string? ExportReportPdf(WeeklyReportViewModel viewModel, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = UiText.L("TitleExportPdf"),
            Filter = UiText.L("FilterPdf"),
            FileName = $"{defaultFileName}.pdf"
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        SaveReportPdf(viewModel, dialog.FileName);
        return dialog.FileName;
    }

    public static void ShareFile(string filePath)
    {
        var message = UiText.L("MsgWhatsAppManualShare");
        var url = $"https://wa.me/?text={Uri.EscapeDataString(message)}";

        if (File.Exists(filePath))
        {
            System.Windows.Clipboard.SetText(filePath);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });

        MessageBox.Show(
            $"{UiText.L("MsgWhatsAppManualShare")}{Environment.NewLine}{Environment.NewLine}{filePath}",
            UiText.L("BtnShare"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static void SaveElementAsPng(FrameworkElement element, string filePath)
    {
        element.UpdateLayout();

        var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));

        if (width == 1 || height == 1)
        {
            element.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Arrange(new Rect(element.DesiredSize));
            width = Math.Max(1, (int)Math.Ceiling(element.DesiredSize.Width));
            height = Math.Max(1, (int)Math.Ceiling(element.DesiredSize.Height));
        }

        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            if (element.FlowDirection == FlowDirection.RightToLeft)
            {
                // RenderTargetBitmap can mirror RTL visuals; flip once here to preserve the on-screen layout.
                drawingContext.PushTransform(new ScaleTransform(-1, 1, width / 2d, height / 2d));
            }

            drawingContext.DrawRectangle(
                new VisualBrush(element)
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                },
                null,
                new Rect(0, 0, width, height));
        }

        var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(drawingVisual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }

    private static void SaveReportPdf(WeeklyReportViewModel viewModel, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(style => style.FontSize(11));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(viewModel.CompanyName).FontSize(16).SemiBold();
                        column.Item().Text(viewModel.ReportTitle).FontSize(22).Bold().FontColor(QuestPDF.Helpers.Colors.Amber.Darken2);
                    });

                    row.ConstantItem(180).AlignRight().Column(column =>
                    {
                        column.Item().Text(UiText.L("LblDate")).SemiBold();
                        column.Item().Text(viewModel.CurrentDateDisplay);
                        column.Item().Text($"{viewModel.FromDate:yyyy/MM/dd} - {viewModel.ToDate:yyyy/MM/dd}");
                    });
                });

                page.Content().PaddingVertical(18).Column(column =>
                {
                    column.Spacing(16);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(card => BuildSummaryCard(card, UiText.L("LblTotalWeightReport"), viewModel.TotalWeightDisplay));
                        row.RelativeItem().Element(card => BuildSummaryCard(card, UiText.L("LblTotalValueReport"), viewModel.TotalValueDisplay));
                        row.RelativeItem().Element(card => BuildSummaryCard(card, UiText.L("LblNetResultReport"), viewModel.NetResultDisplay));
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#F5F6FA").BorderBottom(1).BorderColor("#E1E6EF").Padding(8).Text(UiText.L("LblDate")).SemiBold();
                            header.Cell().Background("#F5F6FA").BorderBottom(1).BorderColor("#E1E6EF").Padding(8).Text(UiText.L("LblType")).SemiBold();
                            header.Cell().Background("#F5F6FA").BorderBottom(1).BorderColor("#E1E6EF").Padding(8).Text(UiText.L("LblWeight")).SemiBold();
                            header.Cell().Background("#F5F6FA").BorderBottom(1).BorderColor("#E1E6EF").Padding(8).Text(UiText.L("LblItem")).SemiBold();
                            header.Cell().Background("#F5F6FA").BorderBottom(1).BorderColor("#E1E6EF").Padding(8).Text(UiText.L("LblValue")).SemiBold();
                        });

                        foreach (var row in viewModel.ReportRows)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#EDF1F7").Padding(8).Text(row.Date.ToString("yyyy/MM/dd"));
                            table.Cell().BorderBottom(1).BorderColor("#EDF1F7").Padding(8).Text(row.Type);
                            table.Cell().BorderBottom(1).BorderColor("#EDF1F7").Padding(8).Text(row.Weight.ToString("0.####"));
                            table.Cell().BorderBottom(1).BorderColor("#EDF1F7").Padding(8).Text(string.IsNullOrWhiteSpace(row.Item) ? "-" : row.Item);
                            table.Cell().BorderBottom(1).BorderColor("#EDF1F7").Padding(8).Text(row.Value.ToString("0.##"));
                        }
                    });

                    column.Item().Background("#FFF8E6").Padding(18).Border(1).BorderColor("#EBD9A4").CornerRadius(18).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(UiText.L("LblTotalGoldReport")).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                            c.Item().Text(viewModel.TotalGoldDisplay).FontSize(18).SemiBold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(UiText.L("LblTotalManufacturingReport")).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                            c.Item().Text(viewModel.TotalManufacturingDisplay).FontSize(18).SemiBold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(UiText.L("LblNetTotalReport")).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                            c.Item().Text(viewModel.NetTotalDisplay).FontSize(18).SemiBold();
                        });
                    });
                });
            });
        }).GeneratePdf(filePath);
    }

    private static void BuildSummaryCard(IContainer container, string label, string value)
    {
        container.Background("#F6F8FC")
            .Border(1)
            .BorderColor("#E6EAF2")
            .CornerRadius(18)
            .Padding(16)
            .Column(column =>
            {
                column.Item().Text(label).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2).FontSize(10);
                column.Item().PaddingTop(8).Text(value).FontSize(20).Bold();
            });
    }

}
