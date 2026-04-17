using System.Collections.ObjectModel;
using System.Windows;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class WeeklyReportViewModel : ViewModelBase
{
    private DateTime _fromDate;
    private DateTime _toDate;
    private string _currentDateDisplay = DateTime.Now.ToString("yyyy/MM/dd hh:mm tt");
    private bool _isUpdatingWeekRange;

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (_isUpdatingWeekRange)
            {
                SetProperty(ref _fromDate, value);
                return;
            }

            UpdateWeekRange(value);
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (_isUpdatingWeekRange)
            {
                SetProperty(ref _toDate, value);
                return;
            }

            UpdateWeekRange(value);
        }
    }

    public ObservableCollection<WeeklyReportRow> Reports { get; } = new();
    public ObservableCollection<ModernReportRow> ReportRows { get; } = new();

    public ObservableCollection<BarItem> TopBalanceBars { get; } = new();

    public string ReportTitle => UiText.L("LblGoldMovementReportTitle");
    public string CompanyName => UiText.L("LblCompanyName");
    public string CurrentDateDisplay
    {
        get => _currentDateDisplay;
        private set => SetProperty(ref _currentDateDisplay, value);
    }
    public string WeekRangeDisplay => UiText.Format("LblWeeklyRangeValue", FromDate.ToString("yyyy/MM/dd"), ToDate.ToString("yyyy/MM/dd"));
    public string WeekRuleNote => UiText.L("LblWeeklyRangeNote");

    public decimal TotalWeight => ReportRows.Sum(row => row.Weight);
    public decimal TotalValue => ReportRows.Sum(row => row.Value);
    public decimal NetResult => Reports.Sum(row => row.TotalGold21);
    public decimal TotalGold => Reports.Sum(row => row.TotalGold21);
    public decimal TotalManufacturing => Reports.Sum(row => row.TotalManufacturing);
    public decimal NetTotal => Reports.Sum(row => row.FinalManufacturing + row.FinalImprovement);

    public string TotalWeightDisplay => $"{TotalWeight:0.####} {UiText.L("LblWeightUnit")}";
    public string TotalValueDisplay => $"{TotalValue:0.##}";
    public string NetResultDisplay => $"{NetResult:0.####} {UiText.L("LblWeightUnit")}";
    public string TotalGoldDisplay => $"{TotalGold:0.####} {UiText.L("LblWeightUnit")}";
    public string TotalManufacturingDisplay => $"{TotalManufacturing:0.##}";
    public string NetTotalDisplay => $"{NetTotal:0.##}";

    public RelayCommand RefreshCommand { get; }
    public RelayCommand ExportImageCommand { get; }
    public RelayCommand ExportPdfCommand { get; }
    public RelayCommand ShareCommand { get; }

    public WeeklyReportViewModel()
    {
        UpdateWeekRange(DateTime.Today);
        RefreshCommand = new RelayCommand(_ => Load());
        ExportImageCommand = new RelayCommand(parameter => ExportImage(parameter as FrameworkElement), _ => ReportRows.Count > 0);
        ExportPdfCommand = new RelayCommand(_ => ExportPdf(), _ => ReportRows.Count > 0);
        ShareCommand = new RelayCommand(parameter => Share(parameter as FrameworkElement), _ => ReportRows.Count > 0);
        Load();
    }

    public void Load()
    {
        if (FromDate > ToDate)
        {
            System.Windows.MessageBox.Show(UiText.L("MsgFromBeforeTo"), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        Reports.Clear();
        ReportRows.Clear();
        CurrentDateDisplay = DateTime.Now.ToString("yyyy/MM/dd hh:mm tt");

        var reportService = AppServices.ReportService;
        var reports = reportService.GetWeeklyReport(FromDate.Date, ToDate.Date);
        var suppliers = AppServices.SupplierService.GetSuppliers().ToDictionary(supplier => supplier.Id, supplier => supplier.Name);
        var transactions = AppServices.TransactionService.GetTransactions(FromDate.Date, ToDate.Date);
        var adjustments = AppServices.OpeningBalanceAdjustmentService.GetAdjustments(FromDate.Date, ToDate.Date);

        foreach (var report in reports)
        {
            Reports.Add(new WeeklyReportRow
            {
                SupplierId = report.SupplierId,
                SupplierName = report.SupplierName,
                TotalGold21 = report.TotalGold21,
                TotalManufacturing = report.TotalManufacturing,
                TotalImprovement = report.TotalImprovement,
                FinalManufacturing = report.TotalManufacturing - report.ManufacturingDiscounts,
                FinalImprovement = report.TotalImprovement - report.ImprovementDiscounts
            });
        }

        foreach (var transaction in transactions.OrderByDescending(transaction => transaction.Date).ThenByDescending(transaction => transaction.Id))
        {
            var type = transaction.Category switch
            {
                GoldShopCore.Models.TransactionCategories.GoldOutbound => UiText.L("LblGoldOutboundReport"),
                GoldShopCore.Models.TransactionCategories.GoldReceipt => UiText.L("LblGoldReceiptReport"),
                GoldShopCore.Models.TransactionCategories.FinishedGoldReceipt => UiText.L("LblFinishedGoldReceiptReport"),
                GoldShopCore.Models.TransactionCategories.CashPayment => UiText.L("LblCashPaymentReport"),
                _ => transaction.Category
            };

            ReportRows.Add(new ModernReportRow
            {
                Date = transaction.Date,
                Type = type,
                Weight = transaction.Equivalent21,
                Item = transaction.ItemName ?? string.Empty,
                Value = transaction.TotalManufacturing + transaction.TotalImprovement,
                SupplierName = suppliers.TryGetValue(transaction.SupplierId, out var supplierName) ? supplierName : string.Empty
            });
        }

        foreach (var adjustment in adjustments.OrderByDescending(item => item.AdjustmentDate).ThenByDescending(item => item.Id))
        {
            ReportRows.Add(new ModernReportRow
            {
                Date = adjustment.AdjustmentDate,
                Type = adjustment.Type == GoldShopCore.Models.OpeningBalanceAdjustmentType.Manufacturing
                    ? UiText.L("LblOpeningBalanceManufacturingAdjustment")
                    : UiText.L("LblOpeningBalanceImprovementAdjustment"),
                Weight = 0m,
                Item = string.IsNullOrWhiteSpace(adjustment.Notes) ? UiText.L("LblOpeningBalanceAdjustmentEntry") : adjustment.Notes!,
                Value = adjustment.Amount,
                SupplierName = suppliers.TryGetValue(adjustment.SupplierId, out var supplierName) ? supplierName : string.Empty
            });
        }

        BuildChart();
        RefreshSummaries();
    }

    private void BuildChart()
    {
        var top = Reports.OrderByDescending(r => r.TotalGold21).Take(10).ToList();
        var max = top.Select(r => r.TotalGold21).DefaultIfEmpty(0m).Max();
        var maxHeight = 180d;

        TopBalanceBars.Clear();
        foreach (var report in top)
        {
            var height = max == 0m ? 0d : (double)(report.TotalGold21 / max) * maxHeight;
            TopBalanceBars.Add(new BarItem
            {
                Label = report.SupplierName,
                Value = (double)report.TotalGold21,
                Height = height
            });
        }

        ExportImageCommand.RaiseCanExecuteChanged();
        ExportPdfCommand.RaiseCanExecuteChanged();
        ShareCommand.RaiseCanExecuteChanged();
    }

    private void RefreshSummaries()
    {
        OnPropertyChanged(nameof(TotalWeight));
        OnPropertyChanged(nameof(TotalValue));
        OnPropertyChanged(nameof(NetResult));
        OnPropertyChanged(nameof(TotalGold));
        OnPropertyChanged(nameof(TotalManufacturing));
        OnPropertyChanged(nameof(NetTotal));
        OnPropertyChanged(nameof(TotalWeightDisplay));
        OnPropertyChanged(nameof(TotalValueDisplay));
        OnPropertyChanged(nameof(NetResultDisplay));
        OnPropertyChanged(nameof(TotalGoldDisplay));
        OnPropertyChanged(nameof(TotalManufacturingDisplay));
        OnPropertyChanged(nameof(NetTotalDisplay));
        OnPropertyChanged(nameof(WeekRangeDisplay));
        OnPropertyChanged(nameof(WeekRuleNote));
    }

    private void UpdateWeekRange(DateTime selectedDate)
    {
        var weekStart = GetWeekStart(selectedDate);
        var weekEnd = weekStart.AddDays(6);

        _isUpdatingWeekRange = true;
        try
        {
            SetProperty(ref _fromDate, weekStart, nameof(FromDate));
            SetProperty(ref _toDate, weekEnd, nameof(ToDate));
        }
        finally
        {
            _isUpdatingWeekRange = false;
        }

        OnPropertyChanged(nameof(WeekRangeDisplay));
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        return date.Date.AddDays(-diff);
    }

    private void ExportImage(FrameworkElement? element)
    {
        if (element == null)
        {
            return;
        }

        var path = ReportExportService.ExportVisualAsPng(element, $"gold-movement-report-{DateTime.Now:yyyyMMdd-HHmm}");
        if (!string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(UiText.Format("MsgReportSaved", path), UiText.L("TitleExportImage"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ExportPdf()
    {
        var path = ReportExportService.ExportReportPdf(this, $"gold-movement-report-{DateTime.Now:yyyyMMdd-HHmm}");
        if (!string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(UiText.Format("MsgReportSaved", path), UiText.L("TitleExportPdf"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Share(FrameworkElement? element)
    {
        if (element == null)
        {
            return;
        }

        var path = ReportExportService.ExportVisualAsShareImage(element, $"gold-movement-report-share-{DateTime.Now:yyyyMMdd-HHmm}");
        ReportExportService.ShareFile(path);
    }
}
