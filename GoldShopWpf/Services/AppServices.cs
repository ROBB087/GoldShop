using GoldShopCore.Data;
using GoldShopCore.Services;

namespace GoldShopWpf.Services;

public static class AppServices
{
    public static SupplierService SupplierService { get; private set; } = null!;
    public static TransactionService TransactionService { get; private set; } = null!;
    public static DiscountService DiscountService { get; private set; } = null!;
    public static ClientNoteService ClientNoteService { get; private set; } = null!;
    public static PricingSettingsService PricingSettingsService { get; private set; } = null!;
    public static ReportService ReportService { get; private set; } = null!;

    public static void Initialize()
    {
        Database.Initialize();

        var supplierRepository = new SupplierRepository();
        var transactionRepository = new TransactionRepository();
        var discountRepository = new DiscountRepository();
        var clientNoteRepository = new ClientNoteRepository();
        var pricingSettingsRepository = new PricingSettingsRepository();

        SupplierService = new SupplierService(supplierRepository, transactionRepository);
        TransactionService = new TransactionService(transactionRepository);
        DiscountService = new DiscountService(discountRepository);
        ClientNoteService = new ClientNoteService(clientNoteRepository);
        PricingSettingsService = new PricingSettingsService(pricingSettingsRepository);
        ReportService = new ReportService(supplierRepository, transactionRepository);
    }
}
