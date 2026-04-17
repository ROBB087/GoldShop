using System.Collections.Concurrent;

namespace GoldShopCore.Services;

public static class FinancialMetricsService
{
    private static readonly ConcurrentDictionary<string, long> Counters = new(StringComparer.Ordinal);

    public static void Increment(string metricName)
    {
        Counters.AddOrUpdate(metricName, 1, static (_, current) => current + 1);
    }

    public static long GetValue(string metricName)
        => Counters.TryGetValue(metricName, out var value) ? value : 0L;

    public static void Reset()
    {
        Counters.Clear();
    }
}
