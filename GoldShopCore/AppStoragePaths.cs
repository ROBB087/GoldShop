namespace GoldShopCore;

public static class AppStoragePaths
{
    private const string ProductFolderName = "GoldShop";
    private static readonly string? RootDirectoryOverride = ResolveRootDirectoryOverride();

    public static string RootDirectory =>
        string.IsNullOrWhiteSpace(RootDirectoryOverride)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductFolderName)
            : Path.GetFullPath(RootDirectoryOverride);

    public static string DataDirectory => Path.Combine(RootDirectory, "Data");

    public static string BackupDirectory => Path.Combine(RootDirectory, "Backups");

    public static string LogDirectory => Path.Combine(RootDirectory, "Logs");

    public static bool IsDevelopmentOverrideActive => !string.IsNullOrWhiteSpace(RootDirectoryOverride);

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(BackupDirectory);
        Directory.CreateDirectory(LogDirectory);
    }

    private static string? ResolveRootDirectoryOverride()
    {
#if DEBUG
        return Environment.GetEnvironmentVariable("GOLDSHOP_APPDATA");
#else
        return null;
#endif
    }
}
