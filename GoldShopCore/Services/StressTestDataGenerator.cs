using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class StressTestDataGenerator
{
    private readonly SupplierRepository _supplierRepository;
    private readonly TransactionRepository _transactionRepository;
    private readonly TraderSummaryRepository _traderSummaryRepository;

    public StressTestDataGenerator(
        SupplierRepository supplierRepository,
        TransactionRepository transactionRepository,
        TraderSummaryRepository traderSummaryRepository)
    {
        _supplierRepository = supplierRepository;
        _transactionRepository = transactionRepository;
        _traderSummaryRepository = traderSummaryRepository;
    }

    public void Generate(int traderCount, int transactionCount)
    {
        if (traderCount <= 0 || transactionCount <= 0)
        {
            throw new ArgumentException("Trader and transaction counts must be greater than zero.");
        }

        var random = new Random(42);
        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        var supplierIds = new List<int>(traderCount);
        for (var i = 1; i <= traderCount; i++)
        {
            supplierIds.Add(_supplierRepository.Add(connection, transaction, new Supplier
            {
                Name = $"Stress Trader {i:D4}",
                Phone = $"010000{i:D4}",
                WorkerName = $"Worker {i:D4}",
                WorkerPhone = $"011000{i:D4}",
                Notes = "Generated for stress testing",
                CreatedAt = DateTime.Today
            }));
        }

        for (var i = 0; i < transactionCount; i++)
        {
            var supplierId = supplierIds[random.Next(supplierIds.Count)];
            var karat = new[] { 18, 21, 24 }[random.Next(3)];
            var originalWeight = decimal.Round((decimal)(random.NextDouble() * 49d) + 1m, 4);
            var equivalent21 = decimal.Round(originalWeight * karat / 21m, 4);
            var category = i % 10 == 0
                ? TransactionCategories.GoldReceipt
                : TransactionCategories.GoldOutbound;

            var transactionModel = new SupplierTransaction
            {
                SupplierId = supplierId,
                Date = DateTime.Today.AddDays(-random.Next(0, 365)),
                Type = category == TransactionCategories.GoldOutbound ? TransactionType.Out : TransactionType.In,
                Category = category,
                ItemName = $"Lot {i + 1:D6}",
                Description = $"Generated stress transaction {i + 1}",
                OriginalWeight = originalWeight,
                OriginalKarat = karat,
                Equivalent21 = equivalent21,
                ManufacturingPerGram = category == TransactionCategories.GoldOutbound ? decimal.Round((decimal)(random.NextDouble() * 20d), 4) : 0m,
                ImprovementPerGram = category == TransactionCategories.GoldOutbound ? decimal.Round((decimal)(random.NextDouble() * 10d), 4) : 0m,
                TotalManufacturing = 0m,
                TotalImprovement = 0m,
                Notes = "Generated for stress testing",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            transactionModel.TotalManufacturing = decimal.Round(transactionModel.OriginalWeight * transactionModel.ManufacturingPerGram, 4);
            transactionModel.TotalImprovement = decimal.Round(transactionModel.Equivalent21 * transactionModel.ImprovementPerGram, 4);

            _transactionRepository.Add(connection, transaction, transactionModel);
        }

        _traderSummaryRepository.RebuildAll(connection, transaction);
        transaction.Commit();

        FileLogService.LogInfo("StressTestDataGenerator", $"Generated {traderCount} traders and {transactionCount} transactions.");
    }
}
