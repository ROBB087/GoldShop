using System.Threading;

namespace GoldShopWpf.Services;

public sealed class StartupGuard : IDisposable
{
    private const string MutexName = @"Local\GoldShop.SingleOfficialInstance";
    private readonly Mutex? _mutex;

    private StartupGuard(Mutex? mutex)
    {
        _mutex = mutex;
    }

    public static StartupGuardResult TryAcquire()
    {
        try
        {
            var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
            return createdNew
                ? new StartupGuardResult(true, null, new StartupGuard(mutex))
                : new StartupGuardResult(false, UiText.L("MsgAppAlreadyRunning", "GoldShop is already running."), null);
        }
        catch (Exception ex)
        {
            GoldShopCore.Services.FileLogService.LogError("Startup mutex acquisition failed", ex);
            return new StartupGuardResult(false, UiText.L("MsgStartupValidationFailed", "The app could not start safely."), null);
        }
    }

    public void Dispose()
    {
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch
        {
            // Ignore shutdown cleanup failures.
        }
        finally
        {
            _mutex?.Dispose();
        }
    }
}

public sealed record StartupGuardResult(bool Succeeded, string? UserMessage, StartupGuard? Guard);
