using GoldShopCore.Data;
using GoldShopCore.Services;
using GoldShopApp.UI;

namespace GoldShopApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Database.Initialize();

        var supplierRepository = new SupplierRepository();
        var transactionRepository = new TransactionRepository();
        var supplierService = new SupplierService(supplierRepository, transactionRepository);
        var transactionService = new TransactionService(transactionRepository);
        var reportService = new ReportService(supplierRepository, transactionRepository);

        Application.Run(new MainForm(supplierService, transactionService, reportService));
    }
}
