using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class OpeningBalanceAdjustmentService
{
    private readonly OpeningBalanceAdjustmentRepository _adjustmentRepository;
    private readonly TraderSummaryRepository _traderSummaryRepository;
    private readonly AuditService _auditService;
    private readonly CacheService _cacheService;

    public OpeningBalanceAdjustmentService(
        OpeningBalanceAdjustmentRepository adjustmentRepository,
        TraderSummaryRepository traderSummaryRepository,
        AuditService auditService,
        CacheService cacheService)
    {
        _adjustmentRepository = adjustmentRepository;
        _traderSummaryRepository = traderSummaryRepository;
        _auditService = auditService;
        _cacheService = cacheService;
    }

    public List<OpeningBalanceAdjustment> GetAdjustments(int supplierId, DateTime? from, DateTime? to)
        => _adjustmentRepository.GetBySupplier(supplierId, from, to);

    public List<OpeningBalanceAdjustment> GetAdjustments(DateTime? from, DateTime? to)
        => _adjustmentRepository.GetAll(from, to);

    public int AddAdjustment(int supplierId, OpeningBalanceAdjustmentType type, decimal amount, DateTime adjustmentDate, string? notes)
    {
        var adjustment = CreateModel(supplierId, type, amount, adjustmentDate, notes, DateTime.Now, DateTime.Now);

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var id = _adjustmentRepository.Add(connection, transaction, adjustment);
        var summarySnapshot = _traderSummaryRepository.ApplyAdjustmentInsert(connection, transaction, adjustment);
        transaction.Commit();
        _cacheService.SetTraderSummary(summarySnapshot);

        adjustment.Id = id;
        _auditService.Log("OpeningBalanceAdjustment", id, "Create", null, adjustment);
        return id;
    }

    public void UpdateAdjustment(int id, int supplierId, OpeningBalanceAdjustmentType type, decimal amount, DateTime adjustmentDate, string? notes)
    {
        var existing = _adjustmentRepository.GetById(id);
        if (existing == null || existing.IsDeleted)
        {
            throw new InvalidOperationException("Opening balance adjustment was not found.");
        }

        var updated = CreateModel(supplierId, type, amount, adjustmentDate, notes, existing.CreatedAt, DateTime.Now);
        updated.Id = id;

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        _adjustmentRepository.Update(connection, transaction, updated);

        var affectedTraderIds = new HashSet<int> { existing.SupplierId, updated.SupplierId };
        foreach (var traderId in affectedTraderIds)
        {
            if (traderId == existing.SupplierId && traderId == updated.SupplierId)
            {
                var snapshot = _traderSummaryRepository.ApplyAdjustmentUpdate(connection, transaction, existing, updated);
                _cacheService.SetTraderSummary(snapshot);
            }
            else
            {
                _traderSummaryRepository.RefreshForTrader(connection, transaction, traderId);
                var refreshed = _traderSummaryRepository.GetByTrader(connection, transaction, traderId);
                if (refreshed != null)
                {
                    _cacheService.SetTraderSummary(refreshed);
                }
            }
        }

        transaction.Commit();
        _auditService.Log("OpeningBalanceAdjustment", id, "Edit", existing, updated);
    }

    public void DeleteAdjustment(int id)
    {
        var existing = _adjustmentRepository.GetById(id);
        if (existing == null || existing.IsDeleted)
        {
            throw new InvalidOperationException("Opening balance adjustment was not found.");
        }

        var deletedAt = DateTime.Now;
        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        _adjustmentRepository.SoftDelete(connection, transaction, id, deletedAt);
        existing.IsDeleted = true;
        existing.DeletedAt = deletedAt;
        existing.UpdatedAt = deletedAt;
        var summarySnapshot = _traderSummaryRepository.ApplyAdjustmentDelete(connection, transaction, existing);
        transaction.Commit();
        _cacheService.SetTraderSummary(summarySnapshot);
        _auditService.Log("OpeningBalanceAdjustment", id, "Delete", existing, null);
    }

    private static OpeningBalanceAdjustment CreateModel(
        int supplierId,
        OpeningBalanceAdjustmentType type,
        decimal amount,
        DateTime adjustmentDate,
        string? notes,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Opening balance adjustment amount must be greater than zero.", nameof(amount));
        }

        return new OpeningBalanceAdjustment
        {
            SupplierId = supplierId,
            Type = type,
            Amount = decimal.Round(amount, 4, MidpointRounding.AwayFromZero),
            AdjustmentDate = adjustmentDate.Date,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
