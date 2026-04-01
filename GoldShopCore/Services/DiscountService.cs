using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class DiscountService
{
    private readonly DiscountRepository _discountRepository;

    public DiscountService(DiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;
    }

    public List<DiscountRecord> GetDiscounts(int supplierId, DateTime? from, DateTime? to)
        => _discountRepository.GetBySupplier(supplierId, from, to);

    public List<DiscountRecord> GetDiscounts(DateTime? from, DateTime? to)
        => _discountRepository.GetAll(from, to);

    public void AddDiscount(int supplierId, DiscountType type, decimal amount, string? notes)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Discount amount must be greater than zero.", nameof(amount));
        }

        var discount = new DiscountRecord
        {
            SupplierId = supplierId,
            Type = type,
            Amount = decimal.Round(amount, 4, MidpointRounding.AwayFromZero),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTime.Now
        };

        _discountRepository.Add(discount);
    }

    public void DeleteDiscount(int id)
    {
        _discountRepository.Delete(id);
    }
}
