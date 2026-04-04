using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class SupplierService
{
    private readonly SupplierRepository _supplierRepository;
    private readonly TransactionRepository _transactionRepository;
    private readonly TraderSummaryRepository _traderSummaryRepository;
    private readonly AuditService _auditService;
    private readonly CacheService _cacheService;

    public SupplierService(
        SupplierRepository supplierRepository,
        TransactionRepository transactionRepository,
        TraderSummaryRepository traderSummaryRepository,
        AuditService auditService,
        CacheService cacheService)
    {
        _supplierRepository = supplierRepository;
        _transactionRepository = transactionRepository;
        _traderSummaryRepository = traderSummaryRepository;
        _auditService = auditService;
        _cacheService = cacheService;
    }

    public List<Supplier> GetSuppliers() => _cacheService.GetSuppliers(_supplierRepository.GetAll);

    public Supplier? GetSupplier(int id) => _supplierRepository.GetById(id);

    public int AddSupplier(string name, string? phone, string? workerName, string? workerPhone, string? notes)
    {
        var supplier = new Supplier
        {
            Name = name.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            WorkerName = string.IsNullOrWhiteSpace(workerName) ? null : workerName.Trim(),
            WorkerPhone = string.IsNullOrWhiteSpace(workerPhone) ? null : workerPhone.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTime.Today
        };

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var id = _supplierRepository.Add(connection, transaction, supplier);
        _traderSummaryRepository.InitializeTrader(connection, transaction, id);
        transaction.Commit();

        supplier.Id = id;
        _cacheService.SetSupplier(supplier);
        _cacheService.SetTraderSummary(new TraderSummarySnapshot
        {
            TraderId = id,
            LastUpdated = DateTime.Now
        });
        _auditService.Log("Supplier", id, "Create", null, supplier);
        return id;
    }

    public void UpdateSupplier(int id, string name, string? phone, string? workerName, string? workerPhone, string? notes)
    {
        var supplier = _supplierRepository.GetById(id);
        if (supplier == null)
        {
            return;
        }

        var oldValues = new Supplier
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Phone = supplier.Phone,
            WorkerName = supplier.WorkerName,
            WorkerPhone = supplier.WorkerPhone,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt
        };

        supplier.Name = name.Trim();
        supplier.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        supplier.WorkerName = string.IsNullOrWhiteSpace(workerName) ? null : workerName.Trim();
        supplier.WorkerPhone = string.IsNullOrWhiteSpace(workerPhone) ? null : workerPhone.Trim();
        supplier.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        _supplierRepository.Update(supplier);
        _cacheService.SetSupplier(supplier);
        _auditService.Log("Supplier", id, "Update", oldValues, supplier);
    }

    public void DeleteSupplier(int id)
    {
        var supplier = _supplierRepository.GetById(id);
        if (supplier == null)
        {
            return;
        }

        _supplierRepository.Delete(id);
        _cacheService.RemoveSupplier(id);
        _cacheService.RemoveTraderSummary(id);
        _auditService.Log("Supplier", id, "Delete", supplier, null);
    }

    public Dictionary<int, decimal> GetNetGold21BySupplier()
        => _cacheService.GetTraderSummaries(_traderSummaryRepository.GetAll).Values.ToDictionary(x => x.TraderId, x => x.TotalEquivalent21);

    public Dictionary<int, decimal> GetTotalGold21BySupplier()
        => _cacheService.GetTraderSummaries(_traderSummaryRepository.GetAll).Values.ToDictionary(x => x.TraderId, x => x.TotalEquivalent21);

    public Dictionary<int, DateTime> GetLastTransactionDates() => _transactionRepository.GetLastTransactionDates();
}
