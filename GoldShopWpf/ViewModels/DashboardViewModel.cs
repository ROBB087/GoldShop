using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using GoldShopCore.Models;
using GoldShopCore.Services;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private decimal _totalOutstanding;
    private int _supplierCount;
    private decimal _totalGoldIssued;
    private decimal _totalPaymentsThisWeek;

    public decimal TotalOutstanding
    {
        get => _totalOutstanding;
        set => SetProperty(ref _totalOutstanding, value);
    }

    public int SupplierCount
    {
        get => _supplierCount;
        set => SetProperty(ref _supplierCount, value);
    }

    public decimal TotalGoldIssued
    {
        get => _totalGoldIssued;
        set => SetProperty(ref _totalGoldIssued, value);
    }

    public decimal TotalPaymentsThisWeek
    {
        get => _totalPaymentsThisWeek;
        set => SetProperty(ref _totalPaymentsThisWeek, value);
    }

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
        var balances = supplierService.GetBalancesBySupplier();
        var totalsAll = transactionService.GetTotalsAll(null, null);

        SupplierCount = suppliers.Count;
        TotalGoldIssued = totalsAll.goldGiven;

        var weekStart = DateTime.Today.AddDays(-7);
        var weekTotals = transactionService.GetTotalsAll(weekStart, DateTime.Today);
        TotalPaymentsThisWeek = weekTotals.paymentsIssued;

        TotalOutstanding = balances.Values.Sum();

        BuildSupplierBalanceChart(suppliers, balances);
        BuildGoldPaymentChart(transactionService);
    }

    private void BuildSupplierBalanceChart(List<Supplier> suppliers, Dictionary<int, decimal> balances)
    {
        SupplierBalanceBars.Clear();

        var topSuppliers = suppliers.OrderByDescending(s => balances.ContainsKey(s.Id) ? balances[s.Id] : 0m).Take(10).ToList();
        var max = topSuppliers.Select(s => balances.ContainsKey(s.Id) ? balances[s.Id] : 0m).DefaultIfEmpty(0m).Max();
        var maxHeight = 180d;

        foreach (var supplier in topSuppliers)
        {
            balances.TryGetValue(supplier.Id, out var balance);
            var height = max == 0m ? 0d : (double)(balance / max) * maxHeight;
            SupplierBalanceBars.Add(new BarItem
            {
                Label = supplier.Name,
                Value = (double)balance,
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
                Gold = g.Where(x => x.Type == TransactionType.GoldGiven).Sum(x => x.Amount),
                Payment = g.Where(x => x.Type == TransactionType.PaymentIssued).Sum(x => x.Amount)
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
