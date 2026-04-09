using GoldShopCore.Data;
using GoldShopCore.Services;

if (args.Length < 2 ||
    !int.TryParse(args[0], out var traderCount) ||
    !int.TryParse(args[1], out var transactionCount))
{
    Console.WriteLine("Usage: GoldShopStressTool <traderCount> <transactionCount> [dbPath]");
    Console.WriteLine(@"Example: GoldShopStressTool 20 300 ""C:\path\to\goldshop.db""");
    return;
}

if (args.Length > 2)
{
    Database.SetDbFilePathOverride(args[2]);
}

Database.Initialize();

var generator = new StressTestDataGenerator(
    new SupplierRepository(),
    new TransactionRepository(),
    new TraderSummaryRepository());

generator.Generate(traderCount, transactionCount);
Console.WriteLine($"Generated {traderCount} traders and {transactionCount} transactions in {Database.DbFilePath}.");
