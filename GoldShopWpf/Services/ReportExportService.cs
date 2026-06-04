using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;
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
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public static string? ExportVisualAsPng(FrameworkElement element, string defaultFileName, string? initialDirectory = null)
    {
        var dialog = new SaveFileDialog
        {
            Title = UiText.L("TitleExportImage"),
            Filter = UiText.L("FilterPng"),
            FileName = $"{defaultFileName}.png",
            InitialDirectory = initialDirectory
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

    public static string ExportReceiptAsWhatsAppImage(FrameworkElement element, string supplierName)
    {
        var folder = GetReceiptFolder(supplierName);
        Directory.CreateDirectory(folder);

        var savedAt = DateTime.Now;
        var fileName = GetReceiptFileName(supplierName, savedAt, "png");
        var filePath = Path.Combine(folder, fileName);

        SaveElementAsPng(element, filePath);
        OpenFolder(folder);
        return filePath;
    }

    public static string GetReceiptFolder()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "\u0627\u0644\u0627\u064a\u0635\u0627\u0644\u0627\u062a");

    public static string GetReceiptFolder(string supplierName)
        => Path.Combine(GetReceiptFolder(), SanitizeFileName(supplierName));

    public static string GetReceiptFileName(string supplierName, DateTime savedAt, string extension)
        => $"\u0643\u0634\u0641-{SanitizeFileName(supplierName)}-\u062a\u0627\u0631\u064a\u062e-{savedAt.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture)}.{extension.TrimStart('.')}";

    public static void OpenReceiptFolder(string supplierName)
    {
        var folder = GetReceiptFolder(supplierName);
        Directory.CreateDirectory(folder);
        OpenFolder(folder);
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
        if (!File.Exists(filePath))
        {
            MessageBox.Show(
                UiText.L("MsgShareFileMissing"),
                UiText.L("BtnShare"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        CopyFileToClipboard(filePath);

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{filePath}\"",
            UseShellExecute = true
        });

        MessageBox.Show(
            $"{UiText.L("MsgShareReport")}{Environment.NewLine}{filePath}{Environment.NewLine}{Environment.NewLine}{UiText.L("MsgShareFileCopied")}",
            UiText.L("BtnShare"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public static void ShareFileViaWhatsApp(string filePath, string? targetPhone)
    {
        if (!File.Exists(filePath))
        {
            MessageBox.Show(
                UiText.L("MsgShareFileMissing"),
                UiText.L("BtnShare"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var normalizedPhone = NormalizePhone(targetPhone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });

            MessageBox.Show(
                UiText.L("MsgWhatsAppMissingPhone"),
                UiText.L("BtnShare"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        CopyFileToClipboard(filePath);

        if (TryFocusExistingWhatsAppWindow())
        {
            return;
        }

        var opened =
            (HasUriSchemeHandler("whatsapp") && TryOpenUri($"whatsapp://send?phone={normalizedPhone}")) ||
            TryOpenUri($"https://web.whatsapp.com/send?phone={normalizedPhone}") ||
            TryOpenUri($"https://api.whatsapp.com/send?phone={normalizedPhone}");

        if (!opened)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });

            MessageBox.Show(
                UiText.L("MsgWhatsAppOpenFailed"),
                UiText.L("BtnShare"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

    }

    private static void SaveElementAsPng(FrameworkElement element, string filePath)
    {
        try
        {
            element.UpdateLayout();
            element.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            var renderSize = new System.Windows.Size(
                Math.Max(element.ActualWidth, element.DesiredSize.Width),
                Math.Max(element.ActualHeight, element.DesiredSize.Height));
            element.Arrange(new Rect(new Point(0, 0), renderSize));
            element.UpdateLayout();

            var width = Math.Max(1, (int)Math.Ceiling(renderSize.Width));
            var height = Math.Max(1, (int)Math.Ceiling(renderSize.Height));

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
        finally
        {
            RestoreElementLayout(element);
        }
    }

    private static void RestoreElementLayout(FrameworkElement element)
    {
        var parent = VisualTreeHelper.GetParent(element) as UIElement;
        element.InvalidateMeasure();
        element.InvalidateArrange();
        parent?.InvalidateMeasure();
        parent?.InvalidateArrange();
        Window.GetWindow(element)?.UpdateLayout();
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

    private static bool TryOpenUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryFocusExistingWhatsAppWindow()
    {
        IntPtr whatsappWindow = IntPtr.Zero;

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
            {
                return true;
            }

            var title = new StringBuilder(512);
            if (GetWindowText(hWnd, title, title.Capacity) == 0)
            {
                return true;
            }

            if (!title.ToString().Contains("WhatsApp", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            whatsappWindow = hWnd;
            return false;
        }, IntPtr.Zero);

        return whatsappWindow != IntPtr.Zero && SetForegroundWindow(whatsappWindow);
    }

    private static bool HasUriSchemeHandler(string scheme)
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey($@"{scheme}\shell\open\command");
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    private static void CopyFileToClipboard(string filePath)
    {
        var files = new StringCollection
        {
            filePath
        };
        Clipboard.SetFileDropList(files);
    }

    private static void OpenFolder(string folderPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{folderPath}\"",
            UseShellExecute = true
        });
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Trim()
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? UiText.L("LblTrader") : sanitized;
    }

    private static string? NormalizePhone(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return null;
        }

        var trimmed = rawPhone.Trim();
        var hasPlusPrefix = trimmed.StartsWith("+", StringComparison.Ordinal);
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        // Egypt local mobile formats (01xxxxxxxxx or 1xxxxxxxxx) -> country code 20.
        if (digits.Length == 11 && digits.StartsWith("0", StringComparison.Ordinal))
        {
            digits = $"2{digits}";
        }
        else if (digits.Length == 10 && digits.StartsWith("1", StringComparison.Ordinal))
        {
            digits = $"20{digits}";
        }

        if (hasPlusPrefix && digits.StartsWith("0", StringComparison.Ordinal))
        {
            digits = digits.TrimStart('0');
        }

        return digits;
    }

}
