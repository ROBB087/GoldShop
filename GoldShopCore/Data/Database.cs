using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public static class Database
{
    private const int CurrentSchemaVersion = 9;
    private static string? _dbFilePathOverride;

    public static string DbFilePath => _dbFilePathOverride ?? Path.Combine(AppStoragePaths.DataDirectory, "goldshop.db");

    public static string ConnectionString => $"Data Source={DbFilePath};Pooling=True;Mode=ReadWriteCreate;Cache=Shared";

    public static void SetDbFilePathOverride(string? dbFilePath)
    {
        _dbFilePathOverride = string.IsNullOrWhiteSpace(dbFilePath)
            ? null
            : Path.GetFullPath(dbFilePath);
    }

    public static void Initialize()
    {
        AppStoragePaths.EnsureDirectories();
        Directory.CreateDirectory(Path.GetDirectoryName(DbFilePath)!);

        using var connection = OpenConnection();
        EnsureMetadataTable(connection);

        var version = GetSchemaVersion(connection);
        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException($"Database schema version {version} is newer than this application supports ({CurrentSchemaVersion}).");
        }

        if (version < 1)
        {
            CreateCoreTables(connection);
            SetSchemaVersion(connection, 1);
            version = 1;
        }

        if (version < 2)
        {
            MigrateToVersion2(connection);
            SetSchemaVersion(connection, 2);
            version = 2;
        }

        if (version < 3)
        {
            MigrateToVersion3(connection);
            SetSchemaVersion(connection, 3);
            version = 3;
        }

        if (version < 4)
        {
            MigrateToVersion4(connection);
            SetSchemaVersion(connection, 4);
            version = 4;
        }

        if (version < 5)
        {
            MigrateToVersion5(connection);
            SetSchemaVersion(connection, 5);
            version = 5;
        }

        if (version < 6)
        {
            MigrateToVersion6(connection);
            SetSchemaVersion(connection, 6);
            version = 6;
        }

        if (version < 7)
        {
            MigrateToVersion7(connection);
            SetSchemaVersion(connection, 7);
            version = 7;
        }

        if (version < 8)
        {
            MigrateToVersion8(connection);
            SetSchemaVersion(connection, 8);
            version = 8;
        }

        if (version < 9)
        {
            MigrateToVersion9(connection);
            SetSchemaVersion(connection, 9);
            version = 9;
        }

        if (version < CurrentSchemaVersion)
        {
            throw new InvalidOperationException($"Unsupported schema migration path. Expected version {CurrentSchemaVersion}, found {version}.");
        }

        EnsureCriticalSchema(connection);
        EnsureIndexes(connection);
        EnsureTriggers(connection);
    }

    public static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = @"
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA busy_timeout = 5000;
PRAGMA temp_store = MEMORY;";
        pragma.ExecuteNonQuery();

        EnsureCriticalSchema(connection);

        return connection;
    }

    public static void FlushAndReleaseFileHandles()
    {
        try
        {
            if (File.Exists(DbFilePath))
            {
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                command.ExecuteNonQuery();
            }
        }
        catch
        {
            // Best-effort only. We'll still clear pooled connections below.
        }

        SqliteConnection.ClearAllPools();
        DeleteSidecarFile($"{DbFilePath}-wal");
        DeleteSidecarFile($"{DbFilePath}-shm");
    }

    private static void EnsureMetadataTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS AppMetadata (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);";
        command.ExecuteNonQuery();
    }

    private static int GetSchemaVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM AppMetadata WHERE Key = 'SchemaVersion' LIMIT 1;";
        var result = command.ExecuteScalar()?.ToString();
        return int.TryParse(result, out var version) ? version : 0;
    }

    private static void SetSchemaVersion(SqliteConnection connection, int version)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO AppMetadata(Key, Value)
VALUES ('SchemaVersion', $value)
ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
        command.Parameters.AddWithValue("$value", version.ToString());
        command.ExecuteNonQuery();
    }

    private static void CreateCoreTables(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Suppliers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Phone TEXT,
    WorkerName TEXT,
    WorkerPhone TEXT,
    Notes TEXT,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ClientNotes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClientName TEXT NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS PricingSettings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DefaultManufacturingPerGram REAL NOT NULL DEFAULT 0,
    DefaultManufacturingPerGram24 REAL NOT NULL DEFAULT 0,
    DefaultImprovementPerGram REAL NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS AuditLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityType TEXT NOT NULL,
    EntityId INTEGER NOT NULL,
    Action TEXT NOT NULL,
    Actor TEXT NOT NULL,
    OldValues TEXT,
    NewValues TEXT,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS TraderSummaries (
    TraderId INTEGER PRIMARY KEY,
    TotalEquivalent21 REAL NOT NULL DEFAULT 0,
    TotalManufacturing REAL NOT NULL DEFAULT 0,
    TotalImprovement REAL NOT NULL DEFAULT 0,
    ManufacturingAdjustments REAL NOT NULL DEFAULT 0,
    ImprovementAdjustments REAL NOT NULL DEFAULT 0,
    ManufacturingDiscounts REAL NOT NULL DEFAULT 0,
    ImprovementDiscounts REAL NOT NULL DEFAULT 0,
    TotalDiscounts REAL NOT NULL DEFAULT 0,
    NetValues REAL NOT NULL DEFAULT 0,
    LastUpdated TEXT NOT NULL,
    FOREIGN KEY (TraderId) REFERENCES Suppliers(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS OpeningBalanceAdjustments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierId INTEGER NOT NULL,
    Type TEXT NOT NULL CHECK(Type IN ('Manufacturing', 'Improvement')),
    Amount REAL NOT NULL CHECK(Amount > 0),
    AdjustmentDate TEXT NOT NULL,
    Notes TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE
);";
        command.ExecuteNonQuery();
    }

    private static void MigrateToVersion2(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();

        CreateCoreTables(connection);

        if (TableExists(connection, "SupplierTransactions"))
        {
            ExecuteNonQuery(connection, "ALTER TABLE SupplierTransactions RENAME TO SupplierTransactions_Legacy;");
        }

        if (TableExists(connection, "Discounts"))
        {
            ExecuteNonQuery(connection, "ALTER TABLE Discounts RENAME TO Discounts_Legacy;");
        }

        CreateVersion2Tables(connection);
        MigrateSupplierTransactions(connection);
        MigrateDiscounts(connection);

        if (TableExists(connection, "SupplierTransactions_Legacy"))
        {
            ExecuteNonQuery(connection, "DROP TABLE SupplierTransactions_Legacy;");
        }

        if (TableExists(connection, "Discounts_Legacy"))
        {
            ExecuteNonQuery(connection, "DROP TABLE Discounts_Legacy;");
        }

        transaction.Commit();
    }

    private static void MigrateToVersion3(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS TraderSummaries (
    TraderId INTEGER PRIMARY KEY,
    TotalEquivalent21 REAL NOT NULL DEFAULT 0,
    TotalManufacturing REAL NOT NULL DEFAULT 0,
    TotalImprovement REAL NOT NULL DEFAULT 0,
    ManufacturingDiscounts REAL NOT NULL DEFAULT 0,
    ImprovementDiscounts REAL NOT NULL DEFAULT 0,
    TotalDiscounts REAL NOT NULL DEFAULT 0,
    NetValues REAL NOT NULL DEFAULT 0,
    LastUpdated TEXT NOT NULL,
    FOREIGN KEY (TraderId) REFERENCES Suppliers(Id) ON DELETE CASCADE
);";
        command.ExecuteNonQuery();

        new TraderSummaryRepository().RebuildAll(connection, transaction);
        transaction.Commit();
    }

    private static void MigrateToVersion4(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        EnsureColumn(connection, transaction, "SupplierTransactions", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "SupplierTransactions", "DeletedAt", "TEXT");
        EnsureColumn(connection, transaction, "Discounts", "UpdatedAt", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, transaction, "Discounts", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "Discounts", "DeletedAt", "TEXT");

        using var discountUpdate = connection.CreateCommand();
        discountUpdate.Transaction = transaction;
        discountUpdate.CommandText = @"
UPDATE Discounts
SET UpdatedAt = CASE
    WHEN UpdatedAt IS NULL OR UpdatedAt = '' THEN CreatedAt
    ELSE UpdatedAt
END;";
        discountUpdate.ExecuteNonQuery();

        new TraderSummaryRepository().RebuildAll(connection, transaction);
        transaction.Commit();
    }

    private static void MigrateToVersion5(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        EnsureColumn(connection, transaction, "PricingSettings", "DefaultManufacturingPerGram24", "REAL NOT NULL DEFAULT 0");
        transaction.Commit();
    }

    private static void MigrateToVersion6(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        EnsureColumn(connection, transaction, "TraderSummaries", "ManufacturingAdjustments", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "TraderSummaries", "ImprovementAdjustments", "REAL NOT NULL DEFAULT 0");

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS OpeningBalanceAdjustments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierId INTEGER NOT NULL,
    Type TEXT NOT NULL CHECK(Type IN ('Manufacturing', 'Improvement')),
    Amount REAL NOT NULL CHECK(Amount > 0),
    AdjustmentDate TEXT NOT NULL,
    Notes TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE
);";
        command.ExecuteNonQuery();

        new TraderSummaryRepository().RebuildAll(connection, transaction);
        transaction.Commit();
    }

    private static void MigrateToVersion7(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();

        using (var renameCommand = connection.CreateCommand())
        {
            renameCommand.Transaction = transaction;
            renameCommand.CommandText = "ALTER TABLE SupplierTransactions RENAME TO SupplierTransactions_V6;";
            renameCommand.ExecuteNonQuery();
        }

        using (var createCommand = connection.CreateCommand())
        {
            createCommand.Transaction = transaction;
            createCommand.CommandText = @"
CREATE TABLE SupplierTransactions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierId INTEGER NOT NULL,
    TxnDate TEXT NOT NULL,
    Type INTEGER NOT NULL CHECK(Type IN (1, 2)),
    ItemName TEXT,
    Description TEXT,
    Amount REAL NOT NULL DEFAULT 0,
    Weight REAL NOT NULL DEFAULT 0 CHECK(Weight >= 0),
    Purity TEXT,
    OriginalWeight REAL NOT NULL DEFAULT 0 CHECK(OriginalWeight >= 0),
    OriginalKarat INTEGER NOT NULL CHECK(OriginalKarat IN (18, 21, 24)),
    Equivalent21 REAL NOT NULL DEFAULT 0 CHECK(Equivalent21 >= 0),
    ManufacturingPerGram REAL NOT NULL DEFAULT 0 CHECK(ManufacturingPerGram >= 0),
    ImprovementPerGram REAL NOT NULL DEFAULT 0 CHECK(ImprovementPerGram >= 0),
    TotalManufacturing REAL NOT NULL DEFAULT 0,
    TotalImprovement REAL NOT NULL DEFAULT 0,
    Category TEXT NOT NULL CHECK(Category IN ('GoldOutbound', 'GoldReceipt', 'FinishedGoldReceipt', 'CashPayment')),
    IdempotencyKey TEXT,
    Notes TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE
);";
            createCommand.ExecuteNonQuery();
        }

        using (var copyCommand = connection.CreateCommand())
        {
            copyCommand.Transaction = transaction;
            copyCommand.CommandText = @"
INSERT INTO SupplierTransactions
    (Id, SupplierId, TxnDate, Type, ItemName, Description, Amount, Weight, Purity, OriginalWeight, OriginalKarat, Equivalent21,
     ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, Category, IdempotencyKey, Notes, IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
SELECT
    Id, SupplierId, TxnDate, Type, ItemName, Description, Amount, Weight, Purity, OriginalWeight, OriginalKarat, Equivalent21,
    ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, Category, NULL, Notes, IsDeleted, DeletedAt, CreatedAt, UpdatedAt
FROM SupplierTransactions_V6;";
            copyCommand.ExecuteNonQuery();
        }

        using (var dropCommand = connection.CreateCommand())
        {
            dropCommand.Transaction = transaction;
            dropCommand.CommandText = "DROP TABLE SupplierTransactions_V6;";
            dropCommand.ExecuteNonQuery();
        }
        new TraderSummaryRepository().RebuildAll(connection, transaction);
        transaction.Commit();
    }

    private static void MigrateToVersion8(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        EnsureColumn(connection, transaction, "SupplierTransactions", "IdempotencyKey", "TEXT");

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"
UPDATE SupplierTransactions
SET
    TotalManufacturing = -ABS(TotalManufacturing),
    TotalImprovement = -ABS(TotalImprovement),
    UpdatedAt = datetime('now')
WHERE Category = 'FinishedGoldReceipt'
  AND IsDeleted = 0;";
            command.ExecuteNonQuery();
        }

        new TraderSummaryRepository().RebuildAll(connection, transaction);
        transaction.Commit();
    }

    private static void MigrateToVersion9(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        EnsureColumn(connection, transaction, "SupplierTransactions", "IdempotencyKey", "TEXT");

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
CREATE UNIQUE INDEX IF NOT EXISTS UX_SupplierTransactions_SupplierId_IdempotencyKey
ON SupplierTransactions(SupplierId, IdempotencyKey)
WHERE IdempotencyKey IS NOT NULL;";
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void CreateVersion2Tables(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS SupplierTransactions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierId INTEGER NOT NULL,
    TxnDate TEXT NOT NULL,
    Type INTEGER NOT NULL CHECK(Type IN (1, 2)),
    ItemName TEXT,
    Description TEXT,
    Amount REAL NOT NULL DEFAULT 0,
    Weight REAL NOT NULL DEFAULT 0 CHECK(Weight >= 0),
    Purity TEXT,
    OriginalWeight REAL NOT NULL DEFAULT 0 CHECK(OriginalWeight >= 0),
    OriginalKarat INTEGER NOT NULL CHECK(OriginalKarat IN (18, 21, 24)),
    Equivalent21 REAL NOT NULL DEFAULT 0 CHECK(Equivalent21 >= 0),
    ManufacturingPerGram REAL NOT NULL DEFAULT 0 CHECK(ManufacturingPerGram >= 0),
    ImprovementPerGram REAL NOT NULL DEFAULT 0 CHECK(ImprovementPerGram >= 0),
    TotalManufacturing REAL NOT NULL DEFAULT 0,
    TotalImprovement REAL NOT NULL DEFAULT 0,
    Category TEXT NOT NULL CHECK(Category IN ('GoldOutbound', 'GoldReceipt', 'FinishedGoldReceipt', 'CashPayment')),
    IdempotencyKey TEXT,
    Notes TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Discounts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierId INTEGER NOT NULL,
    Type TEXT NOT NULL CHECK(Type IN ('Manufacturing', 'Improvement')),
    Amount REAL NOT NULL CHECK(Amount > 0),
    Notes TEXT,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE
);";
        command.ExecuteNonQuery();
    }

    private static void MigrateSupplierTransactions(SqliteConnection connection)
    {
        if (!TableExists(connection, "SupplierTransactions_Legacy"))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO SupplierTransactions
    (Id, SupplierId, TxnDate, Type, ItemName, Description, Amount, Weight, Purity, OriginalWeight, OriginalKarat, Equivalent21,
     ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, Category, IdempotencyKey, Notes, CreatedAt, UpdatedAt)
SELECT
    Id,
    SupplierId,
    COALESCE(NULLIF(TxnDate, ''), substr(CreatedAt, 1, 10), date('now')),
    CASE
        WHEN Type IN ('Out', 'GoldGiven', 'PaymentIssued', 'Gold', '1') THEN 1
        ELSE 2
    END,
    ItemName,
    Description,
    COALESCE(
        CASE
            WHEN Category = 'CashPayment' THEN 0
            ELSE ROUND(COALESCE(NULLIF(Equivalent21, 0), (COALESCE(NULLIF(OriginalWeight, 0), COALESCE(Weight, 0), COALESCE(Amount, 0)) *
                COALESCE(NULLIF(OriginalKarat, 0), CAST(NULLIF(TRIM(Purity), '') AS INTEGER), 21)
            ) / 21.0), 6)
        END,
        0
    ),
    COALESCE(NULLIF(OriginalWeight, 0), COALESCE(Weight, 0), 0),
    CAST(COALESCE(NULLIF(OriginalKarat, 0), CAST(NULLIF(TRIM(Purity), '') AS INTEGER), 21) AS TEXT),
    COALESCE(NULLIF(OriginalWeight, 0), COALESCE(Weight, 0), 0),
    CASE
        WHEN COALESCE(NULLIF(OriginalKarat, 0), CAST(NULLIF(TRIM(Purity), '') AS INTEGER), 21) = 18 THEN 18
        WHEN COALESCE(NULLIF(OriginalKarat, 0), CAST(NULLIF(TRIM(Purity), '') AS INTEGER), 21) = 24 THEN 24
        ELSE 21
    END,
    CASE
        WHEN Category = 'CashPayment' THEN 0
        ELSE ROUND(COALESCE(NULLIF(Equivalent21, 0), (COALESCE(NULLIF(OriginalWeight, 0), COALESCE(Weight, 0), COALESCE(Amount, 0)) *
            COALESCE(NULLIF(OriginalKarat, 0), CAST(NULLIF(TRIM(Purity), '') AS INTEGER), 21)
        ) / 21.0), 6)
    END,
    ABS(COALESCE(ManufacturingPerGram, 0)),
    ABS(COALESCE(ImprovementPerGram, 0)),
    CASE
        WHEN COALESCE(Category, '') = 'CashPayment' THEN -ABS(COALESCE(TotalManufacturing, ManufacturingPerGram, 0))
        WHEN COALESCE(Category, '') = 'GoldReceipt' THEN 0
        ELSE ROUND(COALESCE(NULLIF(OriginalWeight, 0), COALESCE(Weight, 0), 0) * ABS(COALESCE(ManufacturingPerGram, 0)), 6)
    END,
    CASE
        WHEN COALESCE(Category, '') = 'CashPayment' THEN -ABS(COALESCE(TotalImprovement, ImprovementPerGram, 0))
        WHEN COALESCE(Category, '') = 'GoldReceipt' THEN 0
        ELSE ROUND(
            CASE
                WHEN COALESCE(Category, '') = 'CashPayment' THEN 0
                ELSE COALESCE(NULLIF(Equivalent21, 0), (COALESCE(NULLIF(OriginalWeight, 0), COALESCE(Weight, 0), COALESCE(Amount, 0)) *
                    COALESCE(NULLIF(OriginalKarat, 0), CAST(NULLIF(TRIM(Purity), '') AS INTEGER), 21)
                ) / 21.0)
            END * ABS(COALESCE(ImprovementPerGram, 0)),
            6)
    END,
    CASE
        WHEN COALESCE(Category, '') IN ('GoldOutbound', 'GoldReceipt', 'CashPayment') THEN Category
        WHEN Type IN ('Out', 'GoldGiven', 'PaymentIssued', 'Gold', '1') THEN 'GoldOutbound'
        ELSE 'GoldReceipt'
    END,
    NULL,
    Notes,
    COALESCE(NULLIF(CreatedAt, ''), COALESCE(TxnDate, date('now')) || ' 00:00:00'),
    COALESCE(NULLIF(UpdatedAt, ''), COALESCE(CreatedAt, COALESCE(TxnDate, date('now')) || ' 00:00:00'))
FROM SupplierTransactions_Legacy;";
        command.ExecuteNonQuery();
    }

    private static void MigrateDiscounts(SqliteConnection connection)
    {
        if (!TableExists(connection, "Discounts_Legacy"))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Discounts (Id, SupplierId, Type, Amount, Notes, CreatedAt)
SELECT
    Id,
    SupplierId,
    CASE WHEN Type = 'Improvement' THEN 'Improvement' ELSE 'Manufacturing' END,
    ABS(COALESCE(Amount, 0)),
    Notes,
    COALESCE(NULLIF(CreatedAt, ''), datetime('now'))
FROM Discounts_Legacy
WHERE ABS(COALESCE(Amount, 0)) > 0;";
        command.ExecuteNonQuery();
    }

    private static void EnsureIndexes(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_SupplierId ON SupplierTransactions(SupplierId);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_IsDeleted ON SupplierTransactions(IsDeleted);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_TxnDate ON SupplierTransactions(TxnDate DESC);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_Type ON SupplierTransactions(Type);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_Category ON SupplierTransactions(Category);
CREATE UNIQUE INDEX IF NOT EXISTS UX_SupplierTransactions_SupplierId_IdempotencyKey ON SupplierTransactions(SupplierId, IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_IsDeleted_TxnDate_Id ON SupplierTransactions(IsDeleted, TxnDate DESC, Id DESC);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_SupplierId_TxnDate ON SupplierTransactions(SupplierId, IsDeleted, TxnDate DESC);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_SupplierId_Type_TxnDate ON SupplierTransactions(SupplierId, IsDeleted, Type, TxnDate DESC);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_IsDeleted_SupplierId_TxnDate_Id ON SupplierTransactions(IsDeleted, SupplierId, TxnDate DESC, Id DESC);
CREATE INDEX IF NOT EXISTS IX_Discounts_SupplierId ON Discounts(SupplierId);
CREATE INDEX IF NOT EXISTS IX_Discounts_IsDeleted ON Discounts(IsDeleted);
CREATE INDEX IF NOT EXISTS IX_Discounts_CreatedAt ON Discounts(CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_Discounts_SupplierId_Type_CreatedAt ON Discounts(SupplierId, IsDeleted, Type, CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_Discounts_IsDeleted_CreatedAt_Id ON Discounts(IsDeleted, CreatedAt DESC, Id DESC);
CREATE INDEX IF NOT EXISTS IX_Discounts_IsDeleted_SupplierId_CreatedAt_Id ON Discounts(IsDeleted, SupplierId, CreatedAt DESC, Id DESC);
CREATE INDEX IF NOT EXISTS IX_OpeningBalanceAdjustments_SupplierId ON OpeningBalanceAdjustments(SupplierId);
CREATE INDEX IF NOT EXISTS IX_OpeningBalanceAdjustments_IsDeleted ON OpeningBalanceAdjustments(IsDeleted);
CREATE INDEX IF NOT EXISTS IX_OpeningBalanceAdjustments_AdjustmentDate ON OpeningBalanceAdjustments(AdjustmentDate DESC);
CREATE INDEX IF NOT EXISTS IX_OpeningBalanceAdjustments_SupplierId_Type_AdjustmentDate ON OpeningBalanceAdjustments(SupplierId, IsDeleted, Type, AdjustmentDate DESC);
CREATE INDEX IF NOT EXISTS IX_Suppliers_Name ON Suppliers(Name);
CREATE INDEX IF NOT EXISTS IX_ClientNotes_ClientName ON ClientNotes(ClientName);
CREATE INDEX IF NOT EXISTS IX_ClientNotes_CreatedAt ON ClientNotes(CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_PricingSettings_CreatedAt ON PricingSettings(CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId, CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_TraderSummaries_LastUpdated ON TraderSummaries(LastUpdated DESC);";
        command.ExecuteNonQuery();
    }

    private static void EnsureTriggers(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
DROP TRIGGER IF EXISTS TR_SupplierTransactions_Immutable_Update;
DROP TRIGGER IF EXISTS TR_SupplierTransactions_Immutable_Delete;
DROP TRIGGER IF EXISTS TR_Discounts_Immutable_Delete;
DROP TRIGGER IF EXISTS TR_OpeningBalanceAdjustments_Immutable_Delete;

CREATE TRIGGER IF NOT EXISTS TR_SupplierTransactions_PhysicalDelete_Forbidden
BEFORE DELETE ON SupplierTransactions
BEGIN
    SELECT RAISE(ABORT, 'Transactions must be soft-deleted.');
END;

CREATE TRIGGER IF NOT EXISTS TR_Discounts_PhysicalDelete_Forbidden
BEFORE DELETE ON Discounts
BEGIN
    SELECT RAISE(ABORT, 'Discounts must be soft-deleted.');
END;

CREATE TRIGGER IF NOT EXISTS TR_OpeningBalanceAdjustments_PhysicalDelete_Forbidden
BEFORE DELETE ON OpeningBalanceAdjustments
BEGIN
    SELECT RAISE(ABORT, 'Opening balance adjustments must be soft-deleted.');
END;";
        command.ExecuteNonQuery();
    }

    private static void EnsureCriticalSchema(SqliteConnection connection)
    {
        if (!TableExists(connection, "SupplierTransactions"))
        {
            return;
        }

        using var transaction = connection.BeginTransaction();
        EnsureColumn(connection, transaction, "SupplierTransactions", "IdempotencyKey", "TEXT");
        transaction.Commit();
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";
        command.Parameters.AddWithValue("$name", tableName);
        return command.ExecuteScalar() != null;
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void EnsureColumn(SqliteConnection connection, SqliteTransaction transaction, string tableName, string columnName, string columnDefinition)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        reader.Close();

        using var alter = connection.CreateCommand();
        alter.Transaction = transaction;
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alter.ExecuteNonQuery();
    }

    private static void DeleteSidecarFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
