using GoldShopCore.Models;
using GoldShopCore.Data;

namespace GoldShopCore.Services;

public record SupplierWeeklyReport(int SupplierId, string SupplierName, decimal GoldGiven, decimal GoldReceived, decimal PaymentsIssued, decimal PaymentsReceived, decimal CurrentBalance);

public class ReportService
{
    private readonly SupplierRepository _supplierRepository;
    private readonly TransactionRepository _transactionRepository;

    public ReportService(SupplierRepository supplierRepository, TransactionRepository transactionRepository)
    {
        _supplierRepository = supplierRepository;
        _transactionRepository = transactionRepository;
    }

    public List<SupplierWeeklyReport> GetWeeklyReport(DateTime from, DateTime to)
    {
        var suppliers = _supplierRepository.GetAll();
        var balances = _transactionRepository.GetBalancesBySupplier();
        var reports = new List<SupplierWeeklyReport>();

        foreach (var supplier in suppliers)
        {
            var totals = _transactionRepository.GetTotals(supplier.Id, from, to);
            balances.TryGetValue(supplier.Id, out var balance);
            reports.Add(new SupplierWeeklyReport(
                supplier.Id,
                supplier.Name,
                totals.goldGiven,
                totals.goldReceived,
                totals.paymentsIssued,
                totals.paymentsReceived,
                balance));
        }

        return reports;
    }
}
