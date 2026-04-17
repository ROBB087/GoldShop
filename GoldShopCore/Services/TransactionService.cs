using GoldShopCore.Data;
using GoldShopCore.Models;
using Microsoft.Data.Sqlite;
using System.Data;

namespace GoldShopCore.Services;

public class TransactionService
{
    private static readonly HashSet<int> ValidKarats = new([18, 21, 24]);
    private readonly TransactionRepository _transactionRepository;
    private readonly DiscountRepository _discountRepository;
    private readonly TraderSummaryRepository _traderSummaryRepository;
    private readonly AuditService _auditService;
    private readonly CacheService _cacheService;

    public TransactionService(TransactionRepository transactionRepository, DiscountRepository discountRepository, TraderSummaryRepository traderSummaryRepository, AuditService auditService, CacheService cacheService)
    {
        _transactionRepository = transactionRepository;
        _discountRepository = discountRepository;
        _traderSummaryRepository = traderSummaryRepository;
        _auditService = auditService;
        _cacheService = cacheService;
    }

    public List<SupplierTransaction> GetTransactions(int supplierId, DateTime? from, DateTime? to)
        => _transactionRepository.GetBySupplier(supplierId, from, to);

    public List<SupplierTransaction> GetTransactions(DateTime? from, DateTime? to)
        => _transactionRepository.GetAll(from, to);

    public PagedResult<SupplierTransaction> GetTransactionsPage(int? supplierId, DateTime? from, DateTime? to, int pageNumber, int pageSize)
        => _transactionRepository.GetPaged(supplierId, from, to, pageNumber, pageSize);

    public TraderSummary GetSummary(int supplierId, DateTime? from, DateTime? to)
    {
        if (!from.HasValue && !to.HasValue)
        {
            var cached = _cacheService.GetTraderSummary(supplierId, () => _traderSummaryRepository.GetByTrader(supplierId));
            if (cached != null)
            {
                return MapSummary(cached);
            }
        }

        var summary = _transactionRepository.GetSummary(supplierId, from, to);
        var discounts = _discountRepository.GetDiscountTotals(supplierId, from, to);
        summary.ManufacturingDiscounts = discounts.manufacturingDiscounts;
        summary.ImprovementDiscounts = discounts.improvementDiscounts;
        return summary;
    }

    public TraderSummary GetSummaryAll(DateTime? from, DateTime? to)
    {
        if (!from.HasValue && !to.HasValue)
        {
            var snapshots = _cacheService.GetTraderSummaries(_traderSummaryRepository.GetAll).Values;
            return new TraderSummary
            {
                TotalGold21 = snapshots.Sum(item => item.TotalEquivalent21),
                TotalManufacturing = snapshots.Sum(item => item.TotalManufacturing),
                TotalImprovement = snapshots.Sum(item => item.TotalImprovement),
                ManufacturingAdjustments = snapshots.Sum(item => item.ManufacturingAdjustments),
                ImprovementAdjustments = snapshots.Sum(item => item.ImprovementAdjustments),
                ManufacturingDiscounts = snapshots.Sum(item => item.ManufacturingDiscounts),
                ImprovementDiscounts = snapshots.Sum(item => item.ImprovementDiscounts)
            };
        }

        var summary = _transactionRepository.GetSummaryAll(from, to);
        var discounts = _discountRepository.GetDiscountTotalsAll(from, to);
        summary.ManufacturingDiscounts = discounts.manufacturingDiscounts;
        summary.ImprovementDiscounts = discounts.improvementDiscounts;
        return summary;
    }

    public Dictionary<int, decimal> GetTotalGold21BySupplier()
        => _transactionRepository.GetTotalGold21BySupplier();

    public Dictionary<int, decimal> GetNetGold21BySupplier()
        => _transactionRepository.GetNetGold21BySupplier();

    public List<SupplierSummaryRow> GetSupplierSummaries(DateTime? from, DateTime? to)
        => _transactionRepository.GetSupplierSummaries(from, to);

    public List<DailyTransactionTotals> GetDailyTotals(DateTime from, DateTime to)
        => _transactionRepository.GetDailyTotals(from, to);

    public int AddTransaction(
        int supplierId,
        DateTime date,
        string category,
        decimal originalWeight,
        string? itemName,
        int originalKarat,
        decimal manufacturingValue,
        decimal improvementValue,
        string? notes,
        string idempotencyKey)
    {
        var normalizedIdempotencyKey = NormalizeIdempotencyKey(idempotencyKey);
        var now = DateTime.Now;
        var transaction = CreateTransaction(
            supplierId,
            date,
            category,
            originalWeight,
            itemName,
            originalKarat,
            manufacturingValue,
            improvementValue,
            notes,
            now,
            now,
            normalizedIdempotencyKey);

        try
        {
            using var connection = Database.OpenConnection();
            using var sqliteTransaction = connection.BeginTransaction(IsolationLevel.Serializable);

            var existing = _transactionRepository.GetBySupplierAndIdempotencyKey(connection, sqliteTransaction, supplierId, normalizedIdempotencyKey);
            if (existing != null)
            {
                return HandleIdempotentReplay(existing, transaction, normalizedIdempotencyKey);
            }

            try
            {
                var id = _transactionRepository.Add(connection, sqliteTransaction, transaction);
                var summarySnapshot = _traderSummaryRepository.ApplyTransactionInsert(connection, sqliteTransaction, transaction);
                sqliteTransaction.Commit();
                _cacheService.SetTraderSummary(summarySnapshot);

                transaction.Id = id;
                _auditService.Log("SupplierTransaction", id, "Create", null, transaction);
                return id;
            }
            catch (SqliteException ex) when (IsIdempotencyConstraintViolation(ex))
            {
                sqliteTransaction.Rollback();
                var winner = _transactionRepository.GetBySupplierAndIdempotencyKey(supplierId, normalizedIdempotencyKey);
                if (winner != null)
                {
                    return HandleIdempotentReplay(winner, transaction, normalizedIdempotencyKey);
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            FileLogService.LogError("AddTransaction failed", ex);
            throw;
        }
    }

    public void UpdateTransaction(
        int id,
        int supplierId,
        DateTime date,
        string category,
        decimal originalWeight,
        string? itemName,
        int originalKarat,
        decimal manufacturingValue,
        decimal improvementValue,
        string? notes)
    {
        var existing = _transactionRepository.GetById(id);
        if (existing == null || existing.IsDeleted)
        {
            throw new InvalidOperationException("Transaction was not found.");
        }

        var updated = CreateTransaction(
            supplierId,
            date,
            category,
            originalWeight,
            itemName,
            originalKarat,
            manufacturingValue,
            improvementValue,
            notes,
            existing.CreatedAt,
            DateTime.Now,
            existing.IdempotencyKey);
        updated.Id = id;
        updated.IsDeleted = false;

        try
        {
            using var connection = Database.OpenConnection();
            using var sqliteTransaction = connection.BeginTransaction(IsolationLevel.Serializable);
            _transactionRepository.Update(connection, sqliteTransaction, updated);
            var affectedTraderIds = new HashSet<int> { existing.SupplierId, updated.SupplierId };
            foreach (var traderId in affectedTraderIds)
            {
                var snapshot = traderId == existing.SupplierId && traderId == updated.SupplierId
                    ? _traderSummaryRepository.ApplyTransactionUpdate(connection, sqliteTransaction, existing, updated)
                    : null;

                if (snapshot != null)
                {
                    _cacheService.SetTraderSummary(snapshot);
                }
                else
                {
                    _traderSummaryRepository.RefreshForTrader(connection, sqliteTransaction, traderId);
                    var refreshed = _traderSummaryRepository.GetByTrader(connection, sqliteTransaction, traderId);
                    if (refreshed != null)
                    {
                        _cacheService.SetTraderSummary(refreshed);
                    }
                }
            }

            sqliteTransaction.Commit();
            _auditService.Log("SupplierTransaction", id, "Edit", existing, updated);
        }
        catch (Exception ex)
        {
            FileLogService.LogError("UpdateTransaction failed", ex);
            throw;
        }
    }

    public void DeleteTransaction(int id)
    {
        var existing = _transactionRepository.GetById(id);
        if (existing == null || existing.IsDeleted)
        {
            throw new InvalidOperationException("Transaction was not found.");
        }

        var deletedAt = DateTime.Now;
        try
        {
            using var connection = Database.OpenConnection();
            using var sqliteTransaction = connection.BeginTransaction(IsolationLevel.Serializable);
            _transactionRepository.SoftDelete(connection, sqliteTransaction, id, deletedAt);
            existing.IsDeleted = true;
            existing.DeletedAt = deletedAt;
            existing.UpdatedAt = deletedAt;
            var summarySnapshot = _traderSummaryRepository.ApplyTransactionDelete(connection, sqliteTransaction, existing);
            sqliteTransaction.Commit();
            _cacheService.SetTraderSummary(summarySnapshot);
            _auditService.Log("SupplierTransaction", id, "Delete", existing, null);
        }
        catch (Exception ex)
        {
            FileLogService.LogError("DeleteTransaction failed", ex);
            throw;
        }
    }

    private static SupplierTransaction CreateTransaction(
        int supplierId,
        DateTime date,
        string category,
        decimal originalWeight,
        string? itemName,
        int originalKarat,
        decimal manufacturingValue,
        decimal improvementValue,
        string? notes,
        DateTime createdAt,
        DateTime updatedAt,
        string? idempotencyKey)
    {
        category = TransactionCategories.Normalize(category, TransactionType.Out);
        Validate(category, originalWeight, originalKarat, manufacturingValue, improvementValue);

        var type = TransactionCategories.ResolveType(category);
        var roundedWeight = 0m;
        var roundedManufacturing = 0m;
        var roundedImprovement = 0m;
        var equivalent21 = 0m;
        var totalManufacturing = 0m;
        var totalImprovement = 0m;
        string? description;

        if (category == TransactionCategories.CashPayment)
        {
            roundedManufacturing = decimal.Round(manufacturingValue, 4, MidpointRounding.AwayFromZero);
            roundedImprovement = decimal.Round(improvementValue, 4, MidpointRounding.AwayFromZero);
            totalManufacturing = -roundedManufacturing;
            totalImprovement = -roundedImprovement;
            description = BuildCashPaymentTraceabilityText(roundedManufacturing, roundedImprovement);
            originalKarat = 21;
        }
        else
        {
            roundedWeight = decimal.Round(originalWeight, 4, MidpointRounding.AwayFromZero);
            equivalent21 = CalculateEquivalent21(roundedWeight, originalKarat);
            description = BuildTraceabilityText(roundedWeight, originalKarat, equivalent21);

            if (TransactionCategories.SupportsCharges(category) && category != TransactionCategories.CashPayment)
            {
                roundedManufacturing = decimal.Round(manufacturingValue, 4, MidpointRounding.AwayFromZero);
                roundedImprovement = decimal.Round(improvementValue, 4, MidpointRounding.AwayFromZero);
                totalManufacturing = decimal.Round(roundedWeight * roundedManufacturing, 4, MidpointRounding.AwayFromZero);
                totalImprovement = decimal.Round(equivalent21 * roundedImprovement, 4, MidpointRounding.AwayFromZero);

                if (category == TransactionCategories.FinishedGoldReceipt)
                {
                    totalManufacturing = -totalManufacturing;
                    totalImprovement = -totalImprovement;
                }
            }
        }

        return new SupplierTransaction
        {
            SupplierId = supplierId,
            Date = date.Date,
            Type = type,
            Category = category,
            ItemName = string.IsNullOrWhiteSpace(itemName) ? null : itemName.Trim(),
            Description = description,
            OriginalWeight = roundedWeight,
            OriginalKarat = originalKarat,
            Equivalent21 = equivalent21,
            ManufacturingPerGram = roundedManufacturing,
            ImprovementPerGram = roundedImprovement,
            TotalManufacturing = totalManufacturing,
            TotalImprovement = totalImprovement,
            IdempotencyKey = idempotencyKey,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IsDeleted = false,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    private static string NormalizeIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        return idempotencyKey.Trim();
    }

    private static bool IsEquivalentPayload(SupplierTransaction existing, SupplierTransaction candidate)
    {
        return existing.SupplierId == candidate.SupplierId
            && existing.Date == candidate.Date
            && existing.Type == candidate.Type
            && string.Equals(existing.Category, candidate.Category, StringComparison.Ordinal)
            && string.Equals(existing.ItemName ?? string.Empty, candidate.ItemName ?? string.Empty, StringComparison.Ordinal)
            && existing.OriginalWeight == candidate.OriginalWeight
            && existing.OriginalKarat == candidate.OriginalKarat
            && existing.Equivalent21 == candidate.Equivalent21
            && existing.ManufacturingPerGram == candidate.ManufacturingPerGram
            && existing.ImprovementPerGram == candidate.ImprovementPerGram
            && existing.TotalManufacturing == candidate.TotalManufacturing
            && existing.TotalImprovement == candidate.TotalImprovement
            && string.Equals(existing.Notes ?? string.Empty, candidate.Notes ?? string.Empty, StringComparison.Ordinal);
    }

    private static bool IsIdempotencyConstraintViolation(SqliteException exception)
        => exception.SqliteErrorCode == 19
           && (exception.Message.Contains("UX_SupplierTransactions_SupplierId_IdempotencyKey", StringComparison.Ordinal)
               || exception.Message.Contains("SupplierTransactions.SupplierId, SupplierTransactions.IdempotencyKey", StringComparison.Ordinal));

    private static int HandleIdempotentReplay(SupplierTransaction existing, SupplierTransaction candidate, string idempotencyKey)
    {
        if (!IsEquivalentPayload(existing, candidate))
        {
            FinancialMetricsService.Increment("duplicate_attempt_count");
            FileLogService.LogWarning(
                "TransactionService.IdempotencyConflict",
                $"Conflict for supplier {candidate.SupplierId} and key {idempotencyKey}. Existing transaction {existing.Id} has different payload.");
            throw new InvalidOperationException("Idempotency key conflict: the same key was already used with a different payload.");
        }

        FinancialMetricsService.Increment("idempotent_replay_success");
        FileLogService.LogInfo(
            "TransactionService.IdempotencyHit",
            $"Replayed request for supplier {candidate.SupplierId} with idempotency key {idempotencyKey}. Returning existing transaction {existing.Id}.");
        return existing.Id;
    }

    public static decimal CalculateEquivalent21(decimal originalWeight, int originalKarat)
        => decimal.Round((originalWeight * originalKarat) / 21m, 4, MidpointRounding.AwayFromZero);

    public static string BuildTraceabilityText(decimal originalWeight, int originalKarat, decimal equivalent21)
        => $"21K eq {equivalent21:0.####} g from {originalWeight:0.####} g of {originalKarat}K";

    public static string BuildCashPaymentTraceabilityText(decimal manufacturingAmount, decimal improvementAmount)
        => $"Cash payment posted. Manufacturing {manufacturingAmount:0.####}, Refining {improvementAmount:0.####}.";

    public static void Validate(string category, decimal originalWeight, int originalKarat, decimal manufacturingValue, decimal improvementValue)
    {
        category = TransactionCategories.Normalize(category, TransactionType.Out);

        if (category == TransactionCategories.CashPayment)
        {
            if (manufacturingValue < 0)
            {
                throw new ArgumentException("Manufacturing payment must be zero or greater.", nameof(manufacturingValue));
            }

            if (improvementValue < 0)
            {
                throw new ArgumentException("Refining payment must be zero or greater.", nameof(improvementValue));
            }

            if (manufacturingValue <= 0 && improvementValue <= 0)
            {
                throw new ArgumentException("Enter a manufacturing payment, a refining payment, or both.");
            }

            return;
        }

        if (originalWeight <= 0)
        {
            throw new ArgumentException("Weight must be greater than zero.", nameof(originalWeight));
        }

        if (!ValidKarats.Contains(originalKarat))
        {
            throw new ArgumentException("Karat must be one of the supported values: 18, 21, 24.", nameof(originalKarat));
        }

        if (manufacturingValue < 0)
        {
            throw new ArgumentException("Manufacturing value must be zero or greater.", nameof(manufacturingValue));
        }

        if (improvementValue < 0)
        {
            throw new ArgumentException("Refining value must be zero or greater.", nameof(improvementValue));
        }

        if (category == TransactionCategories.GoldReceipt && (manufacturingValue != 0 || improvementValue != 0))
        {
            throw new ArgumentException("Gold receipt cannot include manufacturing or refining values.");
        }
    }

    private static TraderSummary MapSummary(TraderSummarySnapshot snapshot)
    {
        return new TraderSummary
        {
            TotalGold21 = snapshot.TotalEquivalent21,
            TotalManufacturing = snapshot.TotalManufacturing,
            TotalImprovement = snapshot.TotalImprovement,
            ManufacturingAdjustments = snapshot.ManufacturingAdjustments,
            ImprovementAdjustments = snapshot.ImprovementAdjustments,
            ManufacturingDiscounts = snapshot.ManufacturingDiscounts,
            ImprovementDiscounts = snapshot.ImprovementDiscounts
        };
    }
}
