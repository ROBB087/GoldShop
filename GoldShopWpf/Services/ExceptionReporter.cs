using System.IO;
using System.Text;
using System.Windows;

namespace GoldShopWpf.Services;

public static class ExceptionReporter
{
    private static readonly object Sync = new();

    public static void Report(Exception ex, string context)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app-errors.log");
            var text = new StringBuilder()
                .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}")
                .AppendLine(ex.ToString())
                .AppendLine(new string('-', 80))
                .ToString();

            lock (Sync)
            {
                File.AppendAllText(logPath, text);
            }
        }
        catch
        {
            // Best-effort logging only.
        }

        MessageBox.Show(
            $"{context}\n\n{ex.Message}\n\nDetails were written to app-errors.log.",
            "Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
