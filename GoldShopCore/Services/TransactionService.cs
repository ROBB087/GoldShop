using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class TransactionService
{
    private readonly TransactionRepository _transactionRepository;

    public TransactionService(TransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public List<SupplierTransaction> GetTransactions(int supplierId, DateTime? from, DateTime? to)
        => _transactionRepository.GetBySupplier(supplierId, from, to);

    public List<SupplierTransaction> GetTransactions(DateTime? from, DateTime? to)
        => _transactionRepository.GetAll(from, to);

    public void AddTransaction(int supplierId, DateTime date, TransactionType type, string? description, decimal amount, decimal? weight, string? purity, TransactionCategory category, string? notes)
    {
        var transaction = new SupplierTransaction
        {
            SupplierId = supplierId,
            Date = date.Date,
            Type = type,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Amount = amount,
            Weight = weight,
            Purity = string.IsNullOrWhiteSpace(purity) ? null : purity.Trim(),
            Category = category,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
        _transactionRepository.Add(transaction);
    }

    public void UpdateTransaction(int id, int supplierId, DateTime date, TransactionType type, string? description, decimal amount, decimal? weight, string? purity, TransactionCategory category, string? notes)
    {
        var transaction = new SupplierTransaction
        {
            Id = id,
            SupplierId = supplierId,
            Date = date.Date,
            Type = type,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Amount = amount,
            Weight = weight,
            Purity = string.IsNullOrWhiteSpace(purity) ? null : purity.Trim(),
            Category = category,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
        _transactionRepository.Update(transaction);
    }

    public void DeleteTransaction(int id)
    {
        _transactionRepository.Delete(id);
    }

    public (decimal goldGiven, decimal goldReceived, decimal paymentsIssued, decimal paymentsReceived) GetTotals(int supplierId, DateTime? from, DateTime? to)
        => _transactionRepository.GetTotals(supplierId, from, to);

    public (decimal goldGiven, decimal goldReceived, decimal paymentsIssued, decimal paymentsReceived) GetTotalsAll(DateTime? from, DateTime? to)
        => _transactionRepository.GetTotalsAll(from, to);
}
