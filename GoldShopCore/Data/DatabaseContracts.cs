namespace GoldShopCore.Data;

public sealed record DatabaseInitializationResult(
    string DatabasePath,
    bool DatabaseAlreadyExisted,
    int StartingSchemaVersion,
    int CurrentSchemaVersion,
    bool FirstRunInitializationUsed,
    bool MigrationOrUpgradePathUsed,
    IReadOnlyList<int> AppliedMigrations);

public sealed record DatabaseInspectionResult(
    string DatabasePath,
    bool Exists,
    int SchemaVersion,
    bool IntegrityOk,
    bool HasCoreTables,
    long FileSizeBytes,
    int SupplierCount,
    int TransactionCount,
    int DiscountCount,
    int ClientNoteCount,
    int PricingSettingsCount,
    int OpeningBalanceAdjustmentCount);

public sealed class DatabaseCompatibilityException : InvalidOperationException
{
    public DatabaseCompatibilityException(string message, int actualSchemaVersion, int supportedSchemaVersion)
        : base(message)
    {
        ActualSchemaVersion = actualSchemaVersion;
        SupportedSchemaVersion = supportedSchemaVersion;
    }

    public int ActualSchemaVersion { get; }

    public int SupportedSchemaVersion { get; }
}
