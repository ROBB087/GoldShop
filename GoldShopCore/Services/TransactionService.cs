using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class TransactionService
{
    private static readonly HashSet<int> ValidKarats = new([18, 21, 22, 24]);
    private readonly TransactionRepository _transactionRepository;

    public TransactionService(TransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public List<SupplierTransaction> GetTransactions(int supplierId, DateTime? from, DateTime? to)
        => _transactionRepository.GetBySupplier(supplierId, from, to);

    public List<SupplierTransaction> GetTransactions(DateTime? from, DateTime? to)
        => _transactionRepository.GetAll(from, to);

    public TraderSummary GetSummary(int supplierId, DateTime? from, DateTime? to)
        => _transactionRepository.GetSummary(supplierId, from, to);

    public TraderSummary GetSummaryAll(DateTime? from, DateTime? to)
        => _transactionRepository.GetSummaryAll(from, to);

    public Dictionary<int, decimal> GetTotalGold21BySupplier()
        => _transactionRepository.GetTotalGold21BySupplier();

    public Dictionary<int, decimal> GetNetGold21BySupplier()
        => _transactionRepository.GetNetGold21BySupplier();

    public void AddTransaction(
        int supplierId,
        DateTime date,
        TransactionType type,
        decimal originalWeight,
        int originalKarat,
        decimal manufacturingPerGram,
        decimal improvementPerGram,
        string? notes)
    {
        var now = DateTime.Now;
        var transaction = CreateTransaction(
            supplierId,
            date,
            type,
            originalWeight,
            originalKarat,
            manufacturingPerGram,
            improvementPerGram,
            notes,
            now,
            now);

        _transactionRepository.Add(transaction);
    }

    public void UpdateTransaction(
        int id,
        int supplierId,
        DateTime date,
        TransactionType type,
        decimal originalWeight,
        int originalKarat,
        decimal manufacturingPerGram,
        decimal improvementPerGram,
        string? notes)
    {
        var existing = _transactionRepository.GetBySupplier(supplierId, null, null).FirstOrDefault(t => t.Id == id);
        var createdAt = existing?.CreatedAt ?? DateTime.Now;
        var transaction = CreateTransaction(
            supplierId,
            date,
            type,
            originalWeight,
            originalKarat,
            manufacturingPerGram,
            improvementPerGram,
            notes,
            createdAt,
            DateTime.Now);

        transaction.Id = id;
        _transactionRepository.Update(transaction);
    }

    public void DeleteTransaction(int id)
    {
        _transactionRepository.Delete(id);
    }

    private static SupplierTransaction CreateTransaction(
        int supplierId,
        DateTime date,
        TransactionType type,
        decimal originalWeight,
        int originalKarat,
        decimal manufacturingPerGram,
        decimal improvementPerGram,
        string? notes,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Validate(originalWeight, originalKarat, manufacturingPerGram, improvementPerGram);

        var equivalent21 = CalculateEquivalent21(originalWeight, originalKarat);
        var totalManufacturing = decimal.Round(originalWeight * manufacturingPerGram, 4, MidpointRounding.AwayFromZero);
        var totalImprovement = decimal.Round(equivalent21 * improvementPerGram, 4, MidpointRounding.AwayFromZero);

        return new SupplierTransaction
        {
            SupplierId = supplierId,
            Date = date.Date,
            Type = type,
            Description = BuildTraceabilityText(originalWeight, originalKarat, equivalent21),
            OriginalWeight = decimal.Round(originalWeight, 4, MidpointRounding.AwayFromZero),
            OriginalKarat = originalKarat,
            Equivalent21 = equivalent21,
            ManufacturingPerGram = decimal.Round(manufacturingPerGram, 4, MidpointRounding.AwayFromZero),
            ImprovementPerGram = decimal.Round(improvementPerGram, 4, MidpointRounding.AwayFromZero),
            TotalManufacturing = totalManufacturing,
            TotalImprovement = totalImprovement,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public static decimal CalculateEquivalent21(decimal originalWeight, int originalKarat)
        => decimal.Round((originalWeight * originalKarat) / 21m, 4, MidpointRounding.AwayFromZero);

    public static string BuildTraceabilityText(decimal originalWeight, int originalKarat, decimal equivalent21)
        => $"21K eq {equivalent21:0.####} g from {originalWeight:0.####} g of {originalKarat}K";

    public static void Validate(decimal originalWeight, int originalKarat, decimal manufacturingPerGram, decimal improvementPerGram)
    {
        if (originalWeight <= 0)
        {
            throw new ArgumentException("Weight must be greater than zero.", nameof(originalWeight));
        }

        if (!ValidKarats.Contains(originalKarat))
        {
            throw new ArgumentException("Karat must be one of the supported values: 18, 21, 22, 24.", nameof(originalKarat));
        }

        if (manufacturingPerGram < 0)
        {
            throw new ArgumentException("Manufacturing per gram must be zero or greater.", nameof(manufacturingPerGram));
        }

        if (improvementPerGram < 0)
        {
            throw new ArgumentException("Improvement per gram must be zero or greater.", nameof(improvementPerGram));
        }
    }
}
