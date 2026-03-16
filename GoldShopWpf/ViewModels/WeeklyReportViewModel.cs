using System.Collections.ObjectModel;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class WeeklyReportViewModel : ViewModelBase
{
    private DateTime _fromDate = DateTime.Today.AddDays(-7);
    private DateTime _toDate = DateTime.Today;

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public ObservableCollection<WeeklyReportRow> Reports { get; } = new();

    public ObservableCollection<BarItem> TopBalanceBars { get; } = new();

    public RelayCommand RefreshCommand { get; }

    public WeeklyReportViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        Load();
    }

    public void Load()
    {
        Reports.Clear();

        var reportService = AppServices.ReportService;
        var reports = reportService.GetWeeklyReport(FromDate.Date, ToDate.Date);

        foreach (var report in reports)
        {
            Reports.Add(new WeeklyReportRow
            {
                SupplierId = report.SupplierId,
                SupplierName = report.SupplierName,
                TotalGold = report.GoldGiven,
                TotalPayments = report.PaymentsIssued,
                CurrentBalance = report.CurrentBalance
            });
        }

        BuildChart();
    }

    private void BuildChart()
    {
        var top = Reports.OrderByDescending(r => r.CurrentBalance).Take(10).ToList();
        var max = top.Select(r => r.CurrentBalance).DefaultIfEmpty(0m).Max();
        var maxHeight = 180d;

        TopBalanceBars.Clear();
        foreach (var report in top)
        {
            var height = max == 0m ? 0d : (double)(report.CurrentBalance / max) * maxHeight;
            TopBalanceBars.Add(new BarItem
            {
                Label = report.SupplierName,
                Value = (double)report.CurrentBalance,
                Height = height
            });
        }
    }
}
