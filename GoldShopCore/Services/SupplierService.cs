using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class SupplierService
{
    private readonly SupplierRepository _supplierRepository;
    private readonly TransactionRepository _transactionRepository;

    public SupplierService(SupplierRepository supplierRepository, TransactionRepository transactionRepository)
    {
        _supplierRepository = supplierRepository;
        _transactionRepository = transactionRepository;
    }

    public List<Supplier> GetSuppliers() => _supplierRepository.GetAll();

    public Supplier? GetSupplier(int id) => _supplierRepository.GetById(id);

    public int AddSupplier(string name, string? phone, string? notes)
    {
        var supplier = new Supplier
        {
            Name = name.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTime.Today
        };
        return _supplierRepository.Add(supplier);
    }

    public void UpdateSupplier(int id, string name, string? phone, string? notes)
    {
        var supplier = _supplierRepository.GetById(id);
        if (supplier == null)
        {
            return;
        }

        supplier.Name = name.Trim();
        supplier.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        supplier.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        _supplierRepository.Update(supplier);
    }

    public void DeleteSupplier(int id)
    {
        _supplierRepository.Delete(id);
    }

    public Dictionary<int, decimal> GetBalancesBySupplier() => _transactionRepository.GetBalancesBySupplier();

    public Dictionary<int, DateTime> GetLastTransactionDates() => _transactionRepository.GetLastTransactionDates();
}
