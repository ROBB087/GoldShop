using GoldShopCore.Models;
using Xunit.Abstractions;

namespace GoldShopCore.Tests;

public class TransactionFinancialIntegrityTests
{
    private readonly ITestOutputHelper _output;

    public TransactionFinancialIntegrityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> TransactionScenarios()
    {
        yield return [TransactionCategories.GoldOutbound, 10m, 18, 2.34567m, 1.23456m];
        yield return [TransactionCategories.GoldReceipt, 7.77776m, 24, 0m, 0m];
        yield return [TransactionCategories.FinishedGoldReceipt, 12.12559m, 21, 1.11119m, 0.55559m];
        yield return [TransactionCategories.CashPayment, 0m, 21, 23.45679m, 11.11119m];
    }

    [Fact]
    public void TransactionCatalog_ShouldExposeAllSupportedMutationTypes()
    {
        var categories = new[]
        {
            TransactionCategories.GoldOutbound,
            TransactionCategories.GoldReceipt,
            TransactionCategories.FinishedGoldReceipt,
            TransactionCategories.CashPayment
        };

        Assert.Equal(4, categories.Distinct().Count());
        Assert.Contains(TransactionCategories.FinishedGoldReceipt, categories);
        Assert.Contains(TransactionCategories.CashPayment, categories);
    }

    [Theory]
    [MemberData(nameof(TransactionScenarios))]
    public void AddTransaction_ShouldPersistExpectedMathAndSummaries(
        string category,
        decimal originalWeight,
        int originalKarat,
        decimal manufacturingValue,
        decimal improvementValue)
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier($"Trader-{category}");
        var expected = FinancialTestContext.CalculateExpectedTransaction(category, originalWeight, originalKarat, manufacturingValue, improvementValue);

        var id = context.TransactionService.AddTransaction(
            supplierId,
            new DateTime(2026, 4, 17),
            category,
            originalWeight,
            "QA Case",
            originalKarat,
            manufacturingValue,
            improvementValue,
            $"Scenario {category}",
            $"key-{category}");

        var saved = context.TransactionRepository.GetById(id);
        Assert.NotNull(saved);
        Assert.Equal(expected.Type, saved!.Type);
        Assert.Equal(category, saved.Category);
        Assert.Equal(expected.OriginalWeight, saved.OriginalWeight);
        Assert.Equal(expected.OriginalKarat, saved.OriginalKarat);
        Assert.Equal(expected.Equivalent21, saved.Equivalent21);
        Assert.Equal(expected.ManufacturingPerGram, saved.ManufacturingPerGram);
        Assert.Equal(expected.ImprovementPerGram, saved.ImprovementPerGram);
        Assert.Equal(expected.TotalManufacturing, saved.TotalManufacturing);
        Assert.Equal(expected.TotalImprovement, saved.TotalImprovement);

        var paged = context.TransactionService.GetTransactionsPage(supplierId, null, null, 1, 10);
        Assert.Single(paged.Items);
        Assert.Equal(id, paged.Items[0].Id);

        context.AssertSummaryMatchesExpected(supplierId);

        var daily = context.TransactionRepository.GetDailyTotals(new DateTime(2026, 4, 17), new DateTime(2026, 4, 17));
        Assert.Single(daily);
        Assert.Equal(expected.Type == TransactionType.Out ? expected.Equivalent21 : -expected.Equivalent21, daily[0].TotalGold21);
        Assert.Equal(expected.TotalManufacturing + expected.TotalImprovement, daily[0].TotalCharges);

        _output.WriteLine($"[{category}] gold={saved.Equivalent21}, manufacturing={saved.TotalManufacturing}, improvement={saved.TotalImprovement}");
    }

    [Fact]
    public void UpdateDeleteAndRepeatDelete_ShouldKeepBalancesConsistent()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();

        var id = context.TransactionService.AddTransaction(
            supplierId,
            new DateTime(2026, 4, 17),
            TransactionCategories.GoldOutbound,
            15m,
            "Initial",
            18,
            2m,
            1m,
            "seed",
            "update-delete-seed");

        context.TransactionService.UpdateTransaction(
            id,
            supplierId,
            new DateTime(2026, 4, 18),
            TransactionCategories.FinishedGoldReceipt,
            15.55555m,
            "Updated",
            24,
            1.23456m,
            0.65436m,
            "updated");

        var expected = FinancialTestContext.CalculateExpectedTransaction(TransactionCategories.FinishedGoldReceipt, 15.55555m, 24, 1.23456m, 0.65436m);
        var updated = context.TransactionRepository.GetById(id);
        Assert.NotNull(updated);
        Assert.Equal(expected.Equivalent21, updated!.Equivalent21);
        Assert.Equal(expected.TotalManufacturing, updated.TotalManufacturing);
        Assert.Equal(expected.TotalImprovement, updated.TotalImprovement);
        context.AssertSummaryMatchesExpected(supplierId);

        context.TransactionService.DeleteTransaction(id);
        Assert.Empty(context.GetActiveTransactions(supplierId));
        context.AssertSummaryMatchesExpected(supplierId);

        Assert.Throws<InvalidOperationException>(() => context.TransactionService.DeleteTransaction(id));
        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public void DiscountsAndAdjustments_ShouldFlowThroughSummariesWithoutPrecisionMismatch()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();

        context.TransactionService.AddTransaction(
            supplierId,
            new DateTime(2026, 4, 17),
            TransactionCategories.GoldOutbound,
            20m,
            "Outbound",
            21,
            2.22229m,
            1.11119m,
            "charges",
            "discount-adjustment-seed");

        var discountId = context.DiscountService.AddDiscount(supplierId, DiscountType.Manufacturing, 10.55555m, "manufacturing discount");
        context.DiscountService.AddDiscount(supplierId, DiscountType.Improvement, 5.44445m, "improvement discount");
        var adjustmentId = context.OpeningBalanceAdjustmentService.AddAdjustment(supplierId, OpeningBalanceAdjustmentType.Manufacturing, 7.77777m, new DateTime(2026, 4, 18), "opening m");
        context.OpeningBalanceAdjustmentService.AddAdjustment(supplierId, OpeningBalanceAdjustmentType.Improvement, 3.33335m, new DateTime(2026, 4, 18), "opening i");

        context.AssertSummaryMatchesExpected(supplierId);

        context.DiscountService.UpdateDiscount(discountId, supplierId, DiscountType.Manufacturing, 9.99999m, "discount updated");
        context.OpeningBalanceAdjustmentService.UpdateAdjustment(adjustmentId, supplierId, OpeningBalanceAdjustmentType.Manufacturing, 8.88888m, new DateTime(2026, 4, 18), "adjustment updated");
        context.AssertSummaryMatchesExpected(supplierId);

        var summary = context.TransactionService.GetSummary(supplierId, null, null);
        Assert.Equal(FinancialTestContext.Round4(summary.TotalManufacturing - summary.ManufacturingDiscounts), summary.FinalManufacturing);
        Assert.Equal(FinancialTestContext.Round4(summary.TotalImprovement - summary.ImprovementDiscounts), summary.FinalImprovement);

        context.DiscountService.DeleteDiscount(discountId);
        context.OpeningBalanceAdjustmentService.DeleteAdjustment(adjustmentId);
        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public void InvalidInputs_ShouldRejectBadFinancialOperationsWithoutPersistingData()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();

        var invalidOperations = new Action[]
        {
            () => context.TransactionService.AddTransaction(supplierId, DateTime.Today, TransactionCategories.GoldOutbound, 0m, "bad", 21, 1m, 1m, null, "invalid-1"),
            () => context.TransactionService.AddTransaction(supplierId, DateTime.Today, TransactionCategories.GoldOutbound, 1m, "bad", 22, 1m, 1m, null, "invalid-2"),
            () => context.TransactionService.AddTransaction(supplierId, DateTime.Today, TransactionCategories.GoldOutbound, 1m, "bad", 21, -1m, 1m, null, "invalid-3"),
            () => context.TransactionService.AddTransaction(supplierId, DateTime.Today, TransactionCategories.GoldReceipt, 1m, "bad", 21, 1m, 0m, null, "invalid-4"),
            () => context.TransactionService.AddTransaction(supplierId, DateTime.Today, TransactionCategories.CashPayment, 0m, "bad", 21, 0m, 0m, null, "invalid-5"),
            () => context.DiscountService.AddDiscount(supplierId, DiscountType.Manufacturing, 1m, "no available total"),
            () => context.OpeningBalanceAdjustmentService.AddAdjustment(supplierId, OpeningBalanceAdjustmentType.Manufacturing, 0m, DateTime.Today, "invalid"),
            () => context.TransactionService.AddTransaction(supplierId, DateTime.Today, TransactionCategories.GoldOutbound, 1m, "bad", 21, 1m, 1m, null, "")
        };

        foreach (var operation in invalidOperations)
        {
            Assert.ThrowsAny<Exception>(operation);
        }

        Assert.Equal(0, context.GetActiveTransactionCount());
        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public void DiscountLimitAndRollback_ShouldPreventPartialWrites()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();

        context.TransactionService.AddTransaction(
            supplierId,
            DateTime.Today,
            TransactionCategories.GoldOutbound,
            10m,
            "seed",
            21,
            1m,
            1m,
            null,
            "rollback-seed");

        Assert.Throws<ArgumentException>(() => context.DiscountService.AddDiscount(supplierId, DiscountType.Manufacturing, 1000m, "too much"));
        Assert.Empty(context.DiscountRepository.GetBySupplier(supplierId, null, null));

        Assert.ThrowsAny<Exception>(() => context.TransactionService.AddTransaction(
            supplierId + 9999,
            DateTime.Today,
            TransactionCategories.GoldOutbound,
            2m,
            "fk failure",
            21,
            1m,
            1m,
            null,
            "fk-failure"));

        Assert.Single(context.GetActiveTransactions(supplierId));
        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public async Task ConcurrentTransactionAdds_ShouldNotLoseOrDuplicateBalances()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();

        var tasks = Enumerable.Range(1, 20)
            .Select(index => Task.Run(() => context.TransactionService.AddTransaction(
                supplierId,
                new DateTime(2026, 4, 17),
                TransactionCategories.GoldOutbound,
                1m + (index / 100m),
                $"Item-{index}",
                21,
                0.5m,
                0.25m,
                $"parallel-{index}",
                $"parallel-key-{index}")));

        await Task.WhenAll(tasks);

        Assert.Equal(20, context.GetActiveTransactions(supplierId).Count);
        Assert.Equal(20, context.GetAuditCount("SupplierTransaction"));
        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public void DailyTotals_ShouldStayConsistentWithAdjustmentsAndTransactions()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();

        context.TransactionService.AddTransaction(
            supplierId,
            new DateTime(2026, 4, 17),
            TransactionCategories.GoldOutbound,
            5m,
            "day1",
            18,
            2m,
            1m,
            null,
            "daily-seed");

        context.OpeningBalanceAdjustmentService.AddAdjustment(
            supplierId,
            OpeningBalanceAdjustmentType.Improvement,
            4.44445m,
            new DateTime(2026, 4, 18),
            "day2 adjustment");

        var daily = context.TransactionRepository.GetDailyTotals(new DateTime(2026, 4, 17), new DateTime(2026, 4, 18));
        Assert.Equal(2, daily.Count);
        Assert.Equal(new DateTime(2026, 4, 17), daily[0].Date);
        Assert.Equal(new DateTime(2026, 4, 18), daily[1].Date);
        Assert.Equal(0m, daily[1].TotalGold21);
        Assert.Equal(FinancialTestContext.Round4(4.44445m), daily[1].TotalCharges);

        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public void Idempotency_SameRequestTwice_ShouldReturnSameTransactionAndSingleBalanceImpact()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();
        const string key = "idem-same-request";

        var firstId = context.TransactionService.AddTransaction(
            supplierId,
            new DateTime(2026, 4, 17),
            TransactionCategories.GoldOutbound,
            10m,
            "Ring",
            21,
            2m,
            1m,
            "first",
            key);

        var replayId = context.TransactionService.AddTransaction(
            supplierId,
            new DateTime(2026, 4, 17),
            TransactionCategories.GoldOutbound,
            10m,
            "Ring",
            21,
            2m,
            1m,
            "first",
            key);

        Assert.Equal(firstId, replayId);
        Assert.Single(context.GetActiveTransactions(supplierId));
        Assert.Equal(1, context.GetMetric("idempotent_replay_success"));
        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public void Idempotency_SameKeyDifferentPayload_ShouldHardFailWithoutSecondBalanceImpact()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();
        const string key = "idem-conflict";

        context.TransactionService.AddTransaction(
            supplierId,
            DateTime.Today,
            TransactionCategories.GoldOutbound,
            8m,
            "Bracelet",
            21,
            2m,
            1m,
            null,
            key);

        var ex = Assert.Throws<InvalidOperationException>(() => context.TransactionService.AddTransaction(
            supplierId,
            DateTime.Today,
            TransactionCategories.GoldOutbound,
            9m,
            "Bracelet",
            21,
            2m,
            1m,
            null,
            key));

        Assert.Contains("Idempotency key conflict", ex.Message);
        Assert.Single(context.GetActiveTransactions(supplierId));
        Assert.Equal(1, context.GetMetric("duplicate_attempt_count"));
        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public void Idempotency_SameKeyAcrossDifferentTraders_ShouldBeAllowed()
    {
        using var context = new FinancialTestContext();
        var firstTrader = context.CreateSupplier("Trader A");
        var secondTrader = context.CreateSupplier("Trader B");
        const string key = "shared-key-different-traders";

        var firstId = context.TransactionService.AddTransaction(
            firstTrader,
            DateTime.Today,
            TransactionCategories.GoldOutbound,
            3m,
            "A",
            21,
            1m,
            1m,
            null,
            key);

        var secondId = context.TransactionService.AddTransaction(
            secondTrader,
            DateTime.Today,
            TransactionCategories.GoldOutbound,
            4m,
            "B",
            21,
            1m,
            1m,
            null,
            key);

        Assert.NotEqual(firstId, secondId);
        context.AssertSummaryMatchesExpected(firstTrader);
        context.AssertSummaryMatchesExpected(secondTrader);
    }

    [Fact]
    public async Task Idempotency_ConcurrentSameKey_ShouldCreateExactlyOneRecord()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();
        const string key = "idem-concurrent";

        var tasks = Enumerable.Range(0, 12)
            .Select(_ => Task.Run(() => context.TransactionService.AddTransaction(
                supplierId,
                new DateTime(2026, 4, 17),
                TransactionCategories.FinishedGoldReceipt,
                4m,
                "Concurrent",
                21,
                1.5m,
                0.5m,
                "same key",
                key)))
            .ToArray();

        var ids = await Task.WhenAll(tasks);
        Assert.Single(ids.Distinct());
        Assert.Single(context.GetActiveTransactions(supplierId));
        Assert.True(context.GetMetric("idempotent_replay_success") >= 1);
        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public void Idempotency_ReplayAfterDelay_ShouldStillBlockDuplicateBalanceImpact()
    {
        using var context = new FinancialTestContext();
        var supplierId = context.CreateSupplier();
        const string key = "idem-replay-delay";

        var firstId = context.TransactionService.AddTransaction(
            supplierId,
            new DateTime(2026, 4, 17),
            TransactionCategories.CashPayment,
            0m,
            null,
            21,
            50m,
            25m,
            "delayed replay",
            key);

        Thread.Sleep(25);

        var replayId = context.TransactionService.AddTransaction(
            supplierId,
            new DateTime(2026, 4, 17),
            TransactionCategories.CashPayment,
            0m,
            null,
            21,
            50m,
            25m,
            "delayed replay",
            key);

        Assert.Equal(firstId, replayId);
        Assert.Single(context.GetActiveTransactions(supplierId));
        Assert.NotNull(context.GetTransactionByIdempotencyKey(supplierId, key));
        context.AssertSummaryMatchesExpected(supplierId);
    }

    [Fact]
    public async Task Randomized_1000Requests_WithConcurrencyAndRetries_ShouldHaveNoDriftOrDuplication()
    {
        using var context = new FinancialTestContext();
        const int seed = 20260417;
        const int totalRequests = 1000;
        var random = new Random(seed);
        var supplierIds = Enumerable.Range(1, 12)
            .Select(index => context.CreateSupplier($"Random Trader {index}"))
            .ToArray();

        var canonicalRequests = new List<RandomTransactionRequest>();
        var scheduledRequests = new List<RandomTransactionRequest>(totalRequests);

        for (var index = 0; index < totalRequests; index++)
        {
            var reuseExisting = canonicalRequests.Count > 0 && random.NextDouble() < 0.32d;
            if (reuseExisting)
            {
                var existing = canonicalRequests[random.Next(canonicalRequests.Count)];
                scheduledRequests.Add(existing);
                continue;
            }

            var category = random.Next(4) switch
            {
                0 => TransactionCategories.GoldOutbound,
                1 => TransactionCategories.GoldReceipt,
                2 => TransactionCategories.FinishedGoldReceipt,
                _ => TransactionCategories.CashPayment
            };

            var supplierId = supplierIds[random.Next(supplierIds.Length)];
            var request = CreateRandomRequest(random, supplierId, category, canonicalRequests.Count);
            canonicalRequests.Add(request);
            scheduledRequests.Add(request);
        }

        Shuffle(random, scheduledRequests);
        var requestResults = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var resultLock = new object();

        for (var offset = 0; offset < scheduledRequests.Count;)
        {
            var batchSize = Math.Min(scheduledRequests.Count - offset, random.Next(1, 18));
            var batch = scheduledRequests.Skip(offset).Take(batchSize).ToArray();
            offset += batchSize;

            var batchTasks = batch.Select(request => Task.Run(() =>
            {
                var id = context.TransactionService.AddTransaction(
                    request.SupplierId,
                    request.Date,
                    request.Category,
                    request.OriginalWeight,
                    request.ItemName,
                    request.OriginalKarat,
                    request.ManufacturingValue,
                    request.ImprovementValue,
                    request.Notes,
                    request.IdempotencyKey);

                lock (resultLock)
                {
                    if (!requestResults.TryGetValue(request.IdempotencyKey, out var ids))
                    {
                        ids = [];
                        requestResults[request.IdempotencyKey] = ids;
                    }

                    ids.Add(id);
                }
            })).ToArray();

            await Task.WhenAll(batchTasks);
        }

        Assert.Equal(canonicalRequests.Count, requestResults.Count);
        Assert.Equal(canonicalRequests.Count, context.GetActiveTransactionCount());

        foreach (var request in canonicalRequests)
        {
            var ids = requestResults[request.IdempotencyKey];
            Assert.True(ids.Count >= 1);
            Assert.Single(ids.Distinct());

            var persisted = context.GetTransactionByIdempotencyKey(request.SupplierId, request.IdempotencyKey);
            Assert.NotNull(persisted);
            Assert.Equal(ids[0], persisted!.Id);
        }

        var totalReplayCount = scheduledRequests.Count - canonicalRequests.Count;
        Assert.Equal(totalReplayCount, context.GetMetric("idempotent_replay_success"));
        Assert.Equal(0, context.GetMetric("duplicate_attempt_count"));

        foreach (var supplierId in supplierIds)
        {
            context.AssertSummaryMatchesExpected(supplierId);
        }

        var daily = context.TransactionRepository.GetDailyTotals(new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));
        var expectedGlobal = context.ComputeExpectedState();
        Assert.Equal(expectedGlobal.TotalGold21, FinancialTestContext.Round4(daily.Sum(item => item.TotalGold21)));
        Assert.Equal(
            FinancialTestContext.Round4(expectedGlobal.TotalManufacturing + expectedGlobal.TotalImprovement),
            FinancialTestContext.Round4(daily.Sum(item => item.TotalCharges)));

        _output.WriteLine($"Random stress seed: {seed}");
        _output.WriteLine($"Total requests: {totalRequests}");
        _output.WriteLine($"Unique transactions persisted: {canonicalRequests.Count}");
        _output.WriteLine($"Replay requests blocked from duplication: {totalReplayCount}");
    }

    private static RandomTransactionRequest CreateRandomRequest(Random random, int supplierId, string category, int index)
    {
        var date = new DateTime(2026, 4, random.Next(1, 29));
        if (category == TransactionCategories.CashPayment)
        {
            var manufacturing = random.NextDouble() < 0.15d ? 0m : FinancialTestContext.Round4((decimal)(random.NextDouble() * 250d) + 0.1m);
            var improvement = manufacturing == 0m || random.NextDouble() < 0.65d
                ? FinancialTestContext.Round4((decimal)(random.NextDouble() * 175d) + 0.1m)
                : 0m;

            return new RandomTransactionRequest(
                supplierId,
                date,
                category,
                0m,
                null,
                21,
                manufacturing,
                improvement,
                $"cash-{index}",
                $"rnd-{supplierId}-{index}");
        }

        var karat = random.Next(3) switch
        {
            0 => 18,
            1 => 21,
            _ => 24
        };
        var weight = FinancialTestContext.Round4((decimal)(random.NextDouble() * 49d) + 0.1m);
        var manufacturingValue = category == TransactionCategories.GoldReceipt
            ? 0m
            : FinancialTestContext.Round4((decimal)(random.NextDouble() * 15d));
        var improvementValue = category == TransactionCategories.GoldReceipt
            ? 0m
            : FinancialTestContext.Round4((decimal)(random.NextDouble() * 8d));

        return new RandomTransactionRequest(
            supplierId,
            date,
            category,
            weight,
            $"item-{index}",
            karat,
            manufacturingValue,
            improvementValue,
            $"note-{index}",
            $"rnd-{supplierId}-{index}");
    }

    private static void Shuffle<T>(Random random, IList<T> items)
    {
        for (var index = items.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }
    }

    private sealed record RandomTransactionRequest(
        int SupplierId,
        DateTime Date,
        string Category,
        decimal OriginalWeight,
        string? ItemName,
        int OriginalKarat,
        decimal ManufacturingValue,
        decimal ImprovementValue,
        string? Notes,
        string IdempotencyKey);
}
