using GoldShopCore.Models;
using GoldShopCore.Data;

namespace GoldShopCore.Services;

public record SupplierWeeklyReport(
    int SupplierId,
    string SupplierName,
    decimal TotalGold21,
    decimal TotalManufacturing,
    decimal TotalImprovement,
    decimal ManufacturingDiscounts,
    decimal ImprovementDiscounts);

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
        var reports = new List<SupplierWeeklyReport>();

        foreach (var supplier in suppliers)
        {
            var totals = _transactionRepository.GetSummary(supplier.Id, from, to);
            reports.Add(new SupplierWeeklyReport(
                supplier.Id,
                supplier.Name,
                totals.TotalGold21,
                totals.TotalManufacturing,
                totals.TotalImprovement,
                totals.ManufacturingDiscounts,
                totals.ImprovementDiscounts));
        }

        return reports;
    }
}
