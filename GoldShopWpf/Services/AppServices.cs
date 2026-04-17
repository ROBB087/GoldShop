using GoldShopCore.Data;
using GoldShopCore.Services;

namespace GoldShopWpf.Services;

public static class AppServices
{
    public static CacheService CacheService { get; private set; } = null!;
    public static SupplierService SupplierService { get; private set; } = null!;
    public static TransactionService TransactionService { get; private set; } = null!;
    public static DiscountService DiscountService { get; private set; } = null!;
    public static OpeningBalanceAdjustmentService OpeningBalanceAdjustmentService { get; private set; } = null!;
    public static ClientNoteService ClientNoteService { get; private set; } = null!;
    public static PricingSettingsService PricingSettingsService { get; private set; } = null!;
    public static ReportService ReportService { get; private set; } = null!;
    public static AuditService AuditService { get; private set; } = null!;
    public static BackupService BackupService { get; private set; } = null!;

    public static void Initialize()
    {
        Database.Initialize();

        var supplierRepository = new SupplierRepository();
        var transactionRepository = new TransactionRepository();
        var discountRepository = new DiscountRepository();
        var openingBalanceAdjustmentRepository = new OpeningBalanceAdjustmentRepository();
        var clientNoteRepository = new ClientNoteRepository();
        var pricingSettingsRepository = new PricingSettingsRepository();
        var auditLogRepository = new AuditLogRepository();
        var traderSummaryRepository = new TraderSummaryRepository();
        CacheService = new CacheService();

        AuditService = new AuditService(auditLogRepository);

        SupplierService = new SupplierService(supplierRepository, transactionRepository, traderSummaryRepository, AuditService, CacheService);
        TransactionService = new TransactionService(transactionRepository, discountRepository, traderSummaryRepository, AuditService, CacheService);
        DiscountService = new DiscountService(discountRepository, traderSummaryRepository, AuditService, CacheService);
        OpeningBalanceAdjustmentService = new OpeningBalanceAdjustmentService(openingBalanceAdjustmentRepository, traderSummaryRepository, AuditService, CacheService);
        ClientNoteService = new ClientNoteService(clientNoteRepository, AuditService);
        PricingSettingsService = new PricingSettingsService(pricingSettingsRepository, AuditService, CacheService);
        ReportService = new ReportService(supplierRepository, transactionRepository, discountRepository, openingBalanceAdjustmentRepository);
        BackupService = new BackupService();
        CacheService.PreloadSuppliers(supplierRepository.GetAll());
        CacheService.PreloadPricingSettings(pricingSettingsRepository.GetLatest() ?? new GoldShopCore.Models.PricingSettings
        {
            DefaultManufacturingPerGram = 0,
            DefaultManufacturingPerGram24 = 0,
            DefaultImprovementPerGram = 0,
            CreatedAt = DateTime.Now
        });
        CacheService.PreloadTraderSummaries(traderSummaryRepository.GetAll());
        BackupService.EnsureAutomaticBackup();
    }

    public static void RestoreDatabase(string sourcePath)
    {
        BackupService.RestoreBackup(sourcePath);
        Initialize();
    }
}
