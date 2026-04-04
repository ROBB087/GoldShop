using GoldShopCore.Data;
using GoldShopCore.Models;

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
    private readonly DiscountRepository _discountRepository;

    public ReportService(
        SupplierRepository supplierRepository,
        TransactionRepository transactionRepository,
        DiscountRepository discountRepository)
    {
        _supplierRepository = supplierRepository;
        _transactionRepository = transactionRepository;
        _discountRepository = discountRepository;
    }

    public List<SupplierWeeklyReport> GetWeeklyReport(DateTime from, DateTime to)
    {
        var suppliers = _supplierRepository.GetAll().ToDictionary(s => s.Id);
        var transactionSummaries = _transactionRepository.GetSupplierSummaries(from, to)
            .ToDictionary(r => r.SupplierId);
        var discountSummaries = _discountRepository.GetSupplierDiscountSummaries(from, to)
            .ToDictionary(r => r.SupplierId);

        return suppliers.Values
            .Select(supplier =>
            {
                transactionSummaries.TryGetValue(supplier.Id, out var tx);
                discountSummaries.TryGetValue(supplier.Id, out var discount);

                return new SupplierWeeklyReport(
                    supplier.Id,
                    supplier.Name,
                    tx?.TotalGold21 ?? 0m,
                    tx?.TotalManufacturing ?? 0m,
                    tx?.TotalImprovement ?? 0m,
                    discount?.ManufacturingDiscounts ?? 0m,
                    discount?.ImprovementDiscounts ?? 0m);
            })
            .ToList();
    }
}
