using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using GoldShopCore.Models;
using GoldShopCore.Services;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private decimal _totalGold21;
    private int _supplierCount;
    private decimal _totalManufacturing;
    private decimal _totalImprovement;
    private decimal _manufacturingDiscounts;
    private decimal _improvementDiscounts;
    private decimal _totalManufacturingThisWeek;
    private decimal _totalImprovementThisWeek;

    public decimal TotalGold21
    {
        get => _totalGold21;
        set => SetProperty(ref _totalGold21, value);
    }

    public int SupplierCount
    {
        get => _supplierCount;
        set => SetProperty(ref _supplierCount, value);
    }

    public decimal TotalManufacturingThisWeek
    {
        get => _totalManufacturingThisWeek;
        set => SetProperty(ref _totalManufacturingThisWeek, value);
    }

    public decimal TotalImprovementThisWeek
    {
        get => _totalImprovementThisWeek;
        set => SetProperty(ref _totalImprovementThisWeek, value);
    }

    public decimal TotalManufacturing
    {
        get => _totalManufacturing;
        set
        {
            if (SetProperty(ref _totalManufacturing, value))
            {
                OnPropertyChanged(nameof(FinalManufacturing));
            }
        }
    }

    public decimal TotalImprovement
    {
        get => _totalImprovement;
        set
        {
            if (SetProperty(ref _totalImprovement, value))
            {
                OnPropertyChanged(nameof(FinalImprovement));
            }
        }
    }

    public decimal ManufacturingDiscounts
    {
        get => _manufacturingDiscounts;
        set
        {
            if (SetProperty(ref _manufacturingDiscounts, value))
            {
                OnPropertyChanged(nameof(FinalManufacturing));
            }
        }
    }

    public decimal ImprovementDiscounts
    {
        get => _improvementDiscounts;
        set
        {
            if (SetProperty(ref _improvementDiscounts, value))
            {
                OnPropertyChanged(nameof(FinalImprovement));
            }
        }
    }

    public decimal FinalManufacturing => TotalManufacturing - ManufacturingDiscounts;
    public decimal FinalImprovement => TotalImprovement - ImprovementDiscounts;

    public ObservableCollection<BarItem> SupplierBalanceBars { get; } = new();
    public PointCollection GoldLinePoints { get; } = new();
    public PointCollection PaymentLinePoints { get; } = new();
    public ObservableCollection<string> GoldPaymentLabels { get; } = new();

    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddSupplierCommand { get; }
    public RelayCommand AddTransactionCommand { get; }

    public event Action? OpenQuickAddSupplier;
    public event Action? OpenQuickAddTransaction;

    public DashboardViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        AddSupplierCommand = new RelayCommand(_ => OpenQuickAddSupplier?.Invoke());
        AddTransactionCommand = new RelayCommand(_ => OpenQuickAddTransaction?.Invoke());
        Load();
    }

    public void Load()
    {
        var supplierService = AppServices.SupplierService;
        var transactionService = AppServices.TransactionService;

        var suppliers = supplierService.GetSuppliers();
        var totalsAll = transactionService.GetSummaryAll(null, null);
        var goldBySupplier = supplierService.GetTotalGold21BySupplier();

        SupplierCount = suppliers.Count;
        TotalGold21 = totalsAll.TotalGold21;
        TotalManufacturing = totalsAll.TotalManufacturing;
        TotalImprovement = totalsAll.TotalImprovement;
        ManufacturingDiscounts = totalsAll.ManufacturingDiscounts;
        ImprovementDiscounts = totalsAll.ImprovementDiscounts;

        var weekStart = DateTime.Today.AddDays(-7);
        var weekTotals = transactionService.GetSummaryAll(weekStart, DateTime.Today);
        TotalManufacturingThisWeek = weekTotals.FinalManufacturing;
        TotalImprovementThisWeek = weekTotals.FinalImprovement;

        BuildSupplierBalanceChart(suppliers, goldBySupplier);
        BuildGoldPaymentChart(transactionService);
    }

    private void BuildSupplierBalanceChart(List<Supplier> suppliers, Dictionary<int, decimal> totals)
    {
        SupplierBalanceBars.Clear();

        var topSuppliers = suppliers.OrderByDescending(s => totals.ContainsKey(s.Id) ? totals[s.Id] : 0m).Take(10).ToList();
        var max = topSuppliers.Select(s => totals.ContainsKey(s.Id) ? totals[s.Id] : 0m).DefaultIfEmpty(0m).Max();
        var maxHeight = 180d;

        foreach (var supplier in topSuppliers)
        {
            totals.TryGetValue(supplier.Id, out var totalGold21);
            var height = max == 0m ? 0d : (double)(totalGold21 / max) * maxHeight;
            SupplierBalanceBars.Add(new BarItem
            {
                Label = supplier.Name,
                Value = (double)totalGold21,
                Height = height
            });
        }
    }

    private void BuildGoldPaymentChart(TransactionService transactionService)
    {
        var from = DateTime.Today.AddDays(-30);
        var to = DateTime.Today;
        var transactions = transactionService.GetTransactions(from, to);

        var dateGroups = transactions
            .GroupBy(t => t.Date.Date)
            .ToDictionary(g => g.Key, g => new
            {
                Gold = g.Sum(x => x.Equivalent21),
                Payment = g.Sum(x => x.TotalManufacturing + x.TotalImprovement)
            });

        var dates = Enumerable.Range(0, (to - from).Days + 1).Select(offset => from.AddDays(offset)).ToList();

        var goldValues = dates.Select(d => dateGroups.TryGetValue(d, out var v) ? (double)v.Gold : 0d).ToList();
        var paymentValues = dates.Select(d => dateGroups.TryGetValue(d, out var v) ? (double)v.Payment : 0d).ToList();
        var max = new[] { goldValues.Max(), paymentValues.Max(), 1d }.Max();
        var chartHeight = 180d;
        var chartWidth = 520d;

        GoldLinePoints.Clear();
        PaymentLinePoints.Clear();
        GoldPaymentLabels.Clear();

        for (var i = 0; i < dates.Count; i++)
        {
            var x = i == 0 ? 0 : i * (chartWidth / (dates.Count - 1));
            var goldY = chartHeight - (goldValues[i] / max * chartHeight);
            var payY = chartHeight - (paymentValues[i] / max * chartHeight);

            GoldLinePoints.Add(new Point(x, goldY));
            PaymentLinePoints.Add(new Point(x, payY));

            if (i % 5 == 0 || i == dates.Count - 1)
            {
                GoldPaymentLabels.Add(dates[i].ToString("MM-dd"));
            }
            else
            {
                GoldPaymentLabels.Add(string.Empty);
            }
        }
    }
}
