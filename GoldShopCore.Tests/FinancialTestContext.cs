using GoldShopCore.Data;
using GoldShopCore.Models;
using GoldShopCore.Services;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Tests;

internal sealed class FinancialTestContext : IDisposable
{
    private readonly string _rootPath;

    public FinancialTestContext()
    {
        _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);

        DatabasePath = Path.Combine(_rootPath, "goldshop.test.db");
        Database.SetDbFilePathOverride(DatabasePath);
        Database.Initialize();
        FinancialMetricsService.Reset();

        Cache = new CacheService();
        AuditService = new AuditService(new AuditLogRepository());
        TransactionRepository = new TransactionRepository();
        TraderSummaryRepository = new TraderSummaryRepository();
        DiscountRepository = new DiscountRepository();
        AdjustmentRepository = new OpeningBalanceAdjustmentRepository();
        SupplierRepository = new SupplierRepository();

        SupplierService = new SupplierService(SupplierRepository, TransactionRepository, TraderSummaryRepository, AuditService, Cache);
        TransactionService = new TransactionService(TransactionRepository, DiscountRepository, TraderSummaryRepository, AuditService, Cache);
        DiscountService = new DiscountService(DiscountRepository, TraderSummaryRepository, AuditService, Cache);
        OpeningBalanceAdjustmentService = new OpeningBalanceAdjustmentService(AdjustmentRepository, TraderSummaryRepository, AuditService, Cache);
    }

    public string DatabasePath { get; }
    public CacheService Cache { get; }
    public AuditService AuditService { get; }
    public SupplierRepository SupplierRepository { get; }
    public TransactionRepository TransactionRepository { get; }
    public TraderSummaryRepository TraderSummaryRepository { get; }
    public DiscountRepository DiscountRepository { get; }
    public OpeningBalanceAdjustmentRepository AdjustmentRepository { get; }
    public SupplierService SupplierService { get; }
    public TransactionService TransactionService { get; }
    public DiscountService DiscountService { get; }
    public OpeningBalanceAdjustmentService OpeningBalanceAdjustmentService { get; }

    public int CreateSupplier(string name = "Trader QA")
        => SupplierService.AddSupplier(name, "01000000000", "Worker", "01111111111", "QA seed");

    public TraderSummarySnapshot? GetSnapshot(int supplierId) => TraderSummaryRepository.GetByTrader(supplierId);

    public ExpectedFinancialState ComputeExpectedState(int? supplierId = null)
    {
        using var connection = Database.OpenConnection();
        var transactions = new List<RawTransactionRow>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT SupplierId, Type, Category, Equivalent21, TotalManufacturing, TotalImprovement
FROM SupplierTransactions
WHERE IsDeleted = 0" + (supplierId.HasValue ? " AND SupplierId = $supplierId" : string.Empty) + ";";

            if (supplierId.HasValue)
            {
                command.Parameters.AddWithValue("$supplierId", supplierId.Value);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(new RawTransactionRow(
                    reader.GetInt32(0),
                    (TransactionType)reader.GetInt32(1),
                    reader.GetString(2),
                    ReadDecimal(reader, 3),
                    ReadDecimal(reader, 4),
                    ReadDecimal(reader, 5)));
            }
        }

        decimal manufacturingAdjustments = 0m;
        decimal improvementAdjustments = 0m;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT Type, Amount
FROM OpeningBalanceAdjustments
WHERE IsDeleted = 0" + (supplierId.HasValue ? " AND SupplierId = $supplierId" : string.Empty) + ";";

            if (supplierId.HasValue)
            {
                command.Parameters.AddWithValue("$supplierId", supplierId.Value);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var type = Enum.Parse<OpeningBalanceAdjustmentType>(reader.GetString(0));
                var amount = ReadDecimal(reader, 1);
                if (type == OpeningBalanceAdjustmentType.Manufacturing)
                {
                    manufacturingAdjustments += amount;
                }
                else
                {
                    improvementAdjustments += amount;
                }
            }
        }

        decimal manufacturingDiscounts = 0m;
        decimal improvementDiscounts = 0m;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT Type, Amount
FROM Discounts
WHERE IsDeleted = 0" + (supplierId.HasValue ? " AND SupplierId = $supplierId" : string.Empty) + ";";

            if (supplierId.HasValue)
            {
                command.Parameters.AddWithValue("$supplierId", supplierId.Value);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var type = Enum.Parse<DiscountType>(reader.GetString(0));
                var amount = ReadDecimal(reader, 1);
                if (type == DiscountType.Manufacturing)
                {
                    manufacturingDiscounts += amount;
                }
                else
                {
                    improvementDiscounts += amount;
                }
            }
        }

        var totalGold21 = transactions.Sum(transaction => transaction.Type == TransactionType.Out
            ? transaction.Equivalent21
            : -transaction.Equivalent21);

        var totalManufacturing = transactions.Sum(transaction => transaction.TotalManufacturing) + manufacturingAdjustments;
        var totalImprovement = transactions.Sum(transaction => transaction.TotalImprovement) + improvementAdjustments;

        return new ExpectedFinancialState(
            Round4(totalGold21),
            Round4(totalManufacturing),
            Round4(totalImprovement),
            Round4(manufacturingAdjustments),
            Round4(improvementAdjustments),
            Round4(manufacturingDiscounts),
            Round4(improvementDiscounts));
    }

    public void AssertSummaryMatchesExpected(int supplierId)
    {
        var expected = ComputeExpectedState(supplierId);
        var actual = TransactionService.GetSummary(supplierId, null, null);
        AssertEquivalent(expected, actual);

        var snapshot = GetSnapshot(supplierId);
        Assert.NotNull(snapshot);
        Assert.Equal(expected.TotalGold21, snapshot!.TotalEquivalent21);
        Assert.Equal(expected.TotalManufacturing, snapshot.TotalManufacturing);
        Assert.Equal(expected.TotalImprovement, snapshot.TotalImprovement);
        Assert.Equal(expected.ManufacturingAdjustments, snapshot.ManufacturingAdjustments);
        Assert.Equal(expected.ImprovementAdjustments, snapshot.ImprovementAdjustments);
        Assert.Equal(expected.ManufacturingDiscounts, snapshot.ManufacturingDiscounts);
        Assert.Equal(expected.ImprovementDiscounts, snapshot.ImprovementDiscounts);

        var allSummary = TransactionService.GetSummaryAll(null, null);
        AssertEquivalent(ComputeExpectedState(), allSummary);
    }

    public List<SupplierTransaction> GetActiveTransactions(int supplierId)
        => TransactionService.GetTransactions(supplierId, null, null);

    public int GetActiveTransactionCount()
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM SupplierTransactions WHERE IsDeleted = 0;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int GetAuditCount(string entityType)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM AuditLogs WHERE EntityType = $entityType;";
        command.Parameters.AddWithValue("$entityType", entityType);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public long GetMetric(string metricName) => FinancialMetricsService.GetValue(metricName);

    public SupplierTransaction? GetTransactionByIdempotencyKey(int supplierId, string idempotencyKey)
        => TransactionRepository.GetBySupplierAndIdempotencyKey(supplierId, idempotencyKey);

    public void Dispose()
    {
        Database.SetDbFilePathOverride(null);

        if (Directory.Exists(_rootPath))
        {
            try
            {
                Directory.Delete(_rootPath, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public static ExpectedTransaction CalculateExpectedTransaction(
        string category,
        decimal originalWeight,
        int originalKarat,
        decimal manufacturingValue,
        decimal improvementValue)
    {
        var normalized = TransactionCategories.Normalize(category, TransactionType.Out);
        var type = TransactionCategories.ResolveType(normalized);

        if (normalized == TransactionCategories.CashPayment)
        {
            return new ExpectedTransaction(
                type,
                0m,
                21,
                0m,
                Round4(manufacturingValue),
                Round4(improvementValue),
                -Round4(manufacturingValue),
                -Round4(improvementValue));
        }

        var roundedWeight = Round4(originalWeight);
        var equivalent21 = Round4((roundedWeight * originalKarat) / 21m);
        var manufacturingPerGram = normalized == TransactionCategories.GoldReceipt ? 0m : Round4(manufacturingValue);
        var improvementPerGram = normalized == TransactionCategories.GoldReceipt ? 0m : Round4(improvementValue);

        var totalManufacturing = normalized == TransactionCategories.GoldReceipt
            ? 0m
            : Round4(roundedWeight * manufacturingPerGram);
        var totalImprovement = normalized == TransactionCategories.GoldReceipt
            ? 0m
            : Round4(equivalent21 * improvementPerGram);

        if (normalized == TransactionCategories.FinishedGoldReceipt)
        {
            totalManufacturing = -totalManufacturing;
            totalImprovement = -totalImprovement;
        }

        return new ExpectedTransaction(
            type,
            roundedWeight,
            originalKarat,
            equivalent21,
            manufacturingPerGram,
            improvementPerGram,
            totalManufacturing,
            totalImprovement);
    }

    public static decimal Round4(decimal value)
        => decimal.Round(value, 4, MidpointRounding.AwayFromZero);

    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetDouble(ordinal));

    private static void AssertEquivalent(ExpectedFinancialState expected, TraderSummary actual)
    {
        Assert.Equal(expected.TotalGold21, actual.TotalGold21);
        Assert.Equal(expected.TotalManufacturing, actual.TotalManufacturing);
        Assert.Equal(expected.TotalImprovement, actual.TotalImprovement);
        Assert.Equal(expected.ManufacturingAdjustments, actual.ManufacturingAdjustments);
        Assert.Equal(expected.ImprovementAdjustments, actual.ImprovementAdjustments);
        Assert.Equal(expected.ManufacturingDiscounts, actual.ManufacturingDiscounts);
        Assert.Equal(expected.ImprovementDiscounts, actual.ImprovementDiscounts);
        Assert.Equal(expected.TotalManufacturing - expected.ManufacturingDiscounts, actual.FinalManufacturing);
        Assert.Equal(expected.TotalImprovement - expected.ImprovementDiscounts, actual.FinalImprovement);
    }

    private sealed record RawTransactionRow(
        int SupplierId,
        TransactionType Type,
        string Category,
        decimal Equivalent21,
        decimal TotalManufacturing,
        decimal TotalImprovement);
}

internal sealed record ExpectedTransaction(
    TransactionType Type,
    decimal OriginalWeight,
    int OriginalKarat,
    decimal Equivalent21,
    decimal ManufacturingPerGram,
    decimal ImprovementPerGram,
    decimal TotalManufacturing,
    decimal TotalImprovement);

internal sealed record ExpectedFinancialState(
    decimal TotalGold21,
    decimal TotalManufacturing,
    decimal TotalImprovement,
    decimal ManufacturingAdjustments,
    decimal ImprovementAdjustments,
    decimal ManufacturingDiscounts,
    decimal ImprovementDiscounts);
