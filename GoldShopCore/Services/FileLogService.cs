using System.Text;

namespace GoldShopCore.Services;

public static class FileLogService
{
    private static readonly object Sync = new();

    public static void LogError(string context, Exception exception)
    {
        Log("ERROR", context, exception.ToString());
    }

    public static void LogWarning(string context, string message)
    {
        Log("WARN", context, message);
    }

    public static void LogInfo(string context, string message)
    {
        Log("INFO", context, message);
    }

    private static void Log(string level, string context, string message)
    {
        try
        {
            AppStoragePaths.EnsureDirectories();
            var logPath = Path.Combine(AppStoragePaths.LogDirectory, "system.log");
            var text = new StringBuilder()
                .Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ")
                .Append(level).Append(' ')
                .Append(context).AppendLine()
                .AppendLine(message)
                .AppendLine(new string('-', 80))
                .ToString();

            lock (Sync)
            {
                File.AppendAllText(logPath, text, Encoding.UTF8);
            }
        }
        catch
        {
            // Best-effort operational logging only.
        }
    }
}
