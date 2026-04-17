namespace GoldShopCore;

public static class AppStoragePaths
{
    private const string ProductFolderName = "GoldShop";
    private static readonly string? RootDirectoryOverride =
        Environment.GetEnvironmentVariable("GOLDSHOP_APPDATA");

    public static string RootDirectory =>
        string.IsNullOrWhiteSpace(RootDirectoryOverride)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductFolderName)
            : Path.GetFullPath(RootDirectoryOverride);

    public static string DataDirectory => Path.Combine(RootDirectory, "Data");

    public static string BackupDirectory => Path.Combine(RootDirectory, "Backups");

    public static string LogDirectory => Path.Combine(RootDirectory, "Logs");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(BackupDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
