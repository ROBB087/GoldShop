using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class DiscountService
{
    private readonly DiscountRepository _discountRepository;
    private readonly TransactionRepository _transactionRepository;
    private readonly TraderSummaryRepository _traderSummaryRepository;
    private readonly AuditService _auditService;
    private readonly CacheService _cacheService;

    public DiscountService(
        DiscountRepository discountRepository,
        TransactionRepository transactionRepository,
        TraderSummaryRepository traderSummaryRepository,
        AuditService auditService,
        CacheService cacheService)
    {
        _discountRepository = discountRepository;
        _transactionRepository = transactionRepository;
        _traderSummaryRepository = traderSummaryRepository;
        _auditService = auditService;
        _cacheService = cacheService;
    }

    public List<DiscountRecord> GetDiscounts(int supplierId, DateTime? from, DateTime? to)
        => _discountRepository.GetBySupplier(supplierId, from, to);

    public List<DiscountRecord> GetDiscounts(DateTime? from, DateTime? to)
        => _discountRepository.GetAll(from, to);

    public PagedResult<DiscountRecord> GetDiscountsPage(int? supplierId, DateTime? from, DateTime? to, int pageNumber, int pageSize)
        => _discountRepository.GetPaged(supplierId, from, to, pageNumber, pageSize);

    public int AddDiscount(int supplierId, DiscountType type, decimal amount, string? notes, DateTime? from = null, DateTime? to = null)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Discount amount must be greater than zero.", nameof(amount));
        }

        var roundedAmount = decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
        ValidateDiscountLimit(supplierId, type, roundedAmount, from, to);

        var discount = new DiscountRecord
        {
            SupplierId = supplierId,
            Type = type,
            Amount = roundedAmount,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        try
        {
            using var connection = Database.OpenConnection();
            using var sqliteTransaction = connection.BeginTransaction();
            var id = _discountRepository.Add(connection, sqliteTransaction, discount);
            var summarySnapshot = _traderSummaryRepository.ApplyDiscountInsert(connection, sqliteTransaction, discount);
            sqliteTransaction.Commit();
            _cacheService.SetTraderSummary(summarySnapshot);

            discount.Id = id;
            _auditService.Log("Discount", id, "Create", null, discount);
            return id;
        }
        catch (Exception ex)
        {
            FileLogService.LogError("AddDiscount failed", ex);
            throw;
        }
    }

    public void UpdateDiscount(int id, int supplierId, DiscountType type, decimal amount, string? notes, DateTime? from = null, DateTime? to = null)
    {
        var existing = _discountRepository.GetById(id);
        if (existing == null || existing.IsDeleted)
        {
            throw new InvalidOperationException("Discount was not found.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Discount amount must be greater than zero.", nameof(amount));
        }

        var roundedAmount = decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
        ValidateDiscountLimit(supplierId, type, roundedAmount, from, to, existing);

        var updated = new DiscountRecord
        {
            Id = id,
            SupplierId = supplierId,
            Type = type,
            Amount = roundedAmount,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.Now
        };

        try
        {
            using var connection = Database.OpenConnection();
            using var sqliteTransaction = connection.BeginTransaction();
            _discountRepository.Update(connection, sqliteTransaction, updated);

            var affectedTraderIds = new HashSet<int> { existing.SupplierId, updated.SupplierId };
            foreach (var traderId in affectedTraderIds)
            {
                if (traderId == existing.SupplierId && traderId == updated.SupplierId)
                {
                    var snapshot = _traderSummaryRepository.ApplyDiscountUpdate(connection, sqliteTransaction, existing, updated);
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
            _auditService.Log("Discount", id, "Edit", existing, updated);
        }
        catch (Exception ex)
        {
            FileLogService.LogError("UpdateDiscount failed", ex);
            throw;
        }
    }

    public void DeleteDiscount(int id)
    {
        var existing = _discountRepository.GetById(id);
        if (existing == null || existing.IsDeleted)
        {
            throw new InvalidOperationException("Discount was not found.");
        }

        var deletedAt = DateTime.Now;
        try
        {
            using var connection = Database.OpenConnection();
            using var sqliteTransaction = connection.BeginTransaction();
            _discountRepository.SoftDelete(connection, sqliteTransaction, id, deletedAt);
            existing.IsDeleted = true;
            existing.DeletedAt = deletedAt;
            existing.UpdatedAt = deletedAt;
            var summarySnapshot = _traderSummaryRepository.ApplyDiscountDelete(connection, sqliteTransaction, existing);
            sqliteTransaction.Commit();
            _cacheService.SetTraderSummary(summarySnapshot);
            _auditService.Log("Discount", id, "Delete", existing, null);
        }
        catch (Exception ex)
        {
            FileLogService.LogError("DeleteDiscount failed", ex);
            throw;
        }
    }

    public List<SupplierDiscountSummaryRow> GetSupplierDiscountSummaries(DateTime? from, DateTime? to)
        => _discountRepository.GetSupplierDiscountSummaries(from, to);

    private void ValidateDiscountLimit(int supplierId, DiscountType type, decimal amount, DateTime? from, DateTime? to, DiscountRecord? existing = null)
    {
        var summary = _transactionRepository.GetSummary(supplierId, from, to);
        var discountTotals = _discountRepository.GetDiscountTotals(supplierId, from, to);
        if (existing != null && !existing.IsDeleted)
        {
            if (existing.Type == DiscountType.Manufacturing)
            {
                discountTotals.manufacturingDiscounts -= existing.Amount;
            }
            else
            {
                discountTotals.improvementDiscounts -= existing.Amount;
            }
        }

        var available = type switch
        {
            DiscountType.Manufacturing => summary.TotalManufacturing - discountTotals.manufacturingDiscounts,
            DiscountType.Improvement => summary.TotalImprovement - discountTotals.improvementDiscounts,
            _ => 0m
        };

        if (amount > available)
        {
            throw new ArgumentException("Discount cannot exceed the available total.", nameof(amount));
        }
    }
}
