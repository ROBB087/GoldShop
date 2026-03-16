using GoldShopCore.Data;
using GoldShopCore.Services;

namespace GoldShopWpf.Services;

public static class AppServices
{
    public static SupplierService SupplierService { get; private set; } = null!;
    public static TransactionService TransactionService { get; private set; } = null!;
    public static ReportService ReportService { get; private set; } = null!;

    public static void Initialize()
    {
        Database.Initialize();

        var supplierRepository = new SupplierRepository();
        var transactionRepository = new TransactionRepository();

        SupplierService = new SupplierService(supplierRepository, transactionRepository);
        TransactionService = new TransactionService(transactionRepository);
        ReportService = new ReportService(supplierRepository, transactionRepository);
    }
}
