using GoldShopCore.Data;
using GoldShopCore.Services;

if (args.Length < 2 ||
    !int.TryParse(args[0], out var traderCount) ||
    !int.TryParse(args[1], out var transactionCount))
{
    Console.WriteLine("Usage: GoldShopStressTool <traderCount> <transactionCount>");
    return;
}

Database.Initialize();

var generator = new StressTestDataGenerator(
    new SupplierRepository(),
    new TransactionRepository(),
    new TraderSummaryRepository());

generator.Generate(traderCount, transactionCount);
Console.WriteLine($"Generated {traderCount} traders and {transactionCount} transactions.");
