using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public static class Database
{
    public static string DbFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "goldshop.db");

    public static string ConnectionString => $"Data Source={DbFilePath}";

    public static void Initialize()
    {
        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Suppliers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Phone TEXT,
    WorkerName TEXT,
    WorkerPhone TEXT,
    Notes TEXT,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS SupplierTransactions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierId INTEGER NOT NULL,
    TxnDate TEXT NOT NULL,
    Type TEXT NOT NULL,
    ItemName TEXT,
    Description TEXT,
    Amount REAL NOT NULL,
    Weight REAL,
    Purity TEXT,
    OriginalWeight REAL NOT NULL DEFAULT 0,
    OriginalKarat INTEGER NOT NULL DEFAULT 21,
    Equivalent21 REAL NOT NULL DEFAULT 0,
    ManufacturingPerGram REAL NOT NULL DEFAULT 0,
    ImprovementPerGram REAL NOT NULL DEFAULT 0,
    TotalManufacturing REAL NOT NULL DEFAULT 0,
    TotalImprovement REAL NOT NULL DEFAULT 0,
    Category TEXT,
    Notes TEXT,
    CreatedAt TEXT NOT NULL DEFAULT '',
    UpdatedAt TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Discounts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierId INTEGER NOT NULL,
    Type TEXT NOT NULL,
    Amount REAL NOT NULL,
    Notes TEXT,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE
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
    DefaultImprovementPerGram REAL NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_SupplierId ON SupplierTransactions(SupplierId);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_TxnDate ON SupplierTransactions(TxnDate);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_SupplierId_TxnDate ON SupplierTransactions(SupplierId, TxnDate DESC);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_UpdatedAt ON SupplierTransactions(UpdatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_Discounts_SupplierId ON Discounts(SupplierId);
CREATE INDEX IF NOT EXISTS IX_Discounts_CreatedAt ON Discounts(CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_Discounts_SupplierId_CreatedAt ON Discounts(SupplierId, CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_Suppliers_Name ON Suppliers(Name);
CREATE INDEX IF NOT EXISTS IX_ClientNotes_ClientName ON ClientNotes(ClientName);
CREATE INDEX IF NOT EXISTS IX_ClientNotes_CreatedAt ON ClientNotes(CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_PricingSettings_CreatedAt ON PricingSettings(CreatedAt DESC);
";
        command.ExecuteNonQuery();

        EnsureColumn(connection, "SupplierTransactions", "Weight", "REAL");
        EnsureColumn(connection, "SupplierTransactions", "Purity", "TEXT");
        EnsureColumn(connection, "SupplierTransactions", "ItemName", "TEXT");
        EnsureColumn(connection, "SupplierTransactions", "OriginalWeight", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "SupplierTransactions", "OriginalKarat", "INTEGER NOT NULL DEFAULT 21");
        EnsureColumn(connection, "SupplierTransactions", "Equivalent21", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "SupplierTransactions", "ManufacturingPerGram", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "SupplierTransactions", "ImprovementPerGram", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "SupplierTransactions", "TotalManufacturing", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "SupplierTransactions", "TotalImprovement", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "SupplierTransactions", "Category", "TEXT");
        EnsureColumn(connection, "SupplierTransactions", "CreatedAt", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "SupplierTransactions", "UpdatedAt", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Suppliers", "WorkerName", "TEXT");
        EnsureColumn(connection, "Suppliers", "WorkerPhone", "TEXT");
        EnsureColumn(connection, "PricingSettings", "DefaultManufacturingPerGram", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "PricingSettings", "DefaultImprovementPerGram", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "PricingSettings", "CreatedAt", "TEXT NOT NULL DEFAULT ''");

        using var migrate = connection.CreateCommand();
        migrate.CommandText = @"
UPDATE SupplierTransactions SET Type = 'Out' WHERE Type IN ('GoldGiven', 'PaymentIssued', 'Gold');
UPDATE SupplierTransactions SET Type = 'In' WHERE Type IN ('GoldReceived', 'PaymentReceived', 'Payment');

UPDATE SupplierTransactions
SET Category = CASE
    WHEN Type = 'Out' THEN 'GoldOutbound'
    WHEN Type = 'In' THEN 'GoldReceipt'
    ELSE 'GoldOutbound'
END
WHERE TRIM(COALESCE(Category, '')) = '';

UPDATE SupplierTransactions
SET OriginalWeight = COALESCE(NULLIF(OriginalWeight, 0), Weight, Amount, 0);

UPDATE SupplierTransactions
SET OriginalKarat = CASE
    WHEN OriginalKarat IS NULL OR OriginalKarat = 0 THEN
        COALESCE(CAST(NULLIF(TRIM(Purity), '') AS INTEGER), 21)
    ELSE OriginalKarat
END;

UPDATE SupplierTransactions
SET Equivalent21 = ROUND((OriginalWeight * OriginalKarat) / 21.0, 6)
WHERE Equivalent21 IS NULL OR Equivalent21 = 0;

UPDATE SupplierTransactions
SET TotalManufacturing = ROUND(OriginalWeight * ManufacturingPerGram, 6)
WHERE TotalManufacturing IS NULL OR TotalManufacturing = 0;

UPDATE SupplierTransactions
SET TotalImprovement = ROUND(Equivalent21 * ImprovementPerGram, 6)
WHERE TotalImprovement IS NULL OR TotalImprovement = 0;

UPDATE SupplierTransactions
SET CreatedAt = CASE
    WHEN TRIM(COALESCE(CreatedAt, '')) = '' THEN TxnDate || ' 00:00:00'
    ELSE CreatedAt
END;

UPDATE SupplierTransactions
SET UpdatedAt = CASE
    WHEN TRIM(COALESCE(UpdatedAt, '')) = '' THEN COALESCE(NULLIF(CreatedAt, ''), TxnDate || ' 00:00:00')
    ELSE UpdatedAt
END;
";
        migrate.ExecuteNonQuery();
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string definition)
    {
        using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table});";
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        alter.ExecuteNonQuery();
    }
}
