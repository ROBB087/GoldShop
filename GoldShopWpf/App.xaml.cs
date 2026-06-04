using System.Windows;
using System.Windows.Threading;
using GoldShopCore.Data;
using GoldShopWpf.Services;

namespace GoldShopWpf;

public partial class App : Application
{
    private StartupGuard? _startupGuard;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        base.OnStartup(e);
        LocalizationService.SetLanguage("ar");

        var guardResult = StartupGuard.TryAcquire();
        if (!guardResult.Succeeded)
        {
            MessageBox.Show(guardResult.UserMessage, UiText.L("TitleApplicationError"), MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _startupGuard = guardResult.Guard;

        if (!StartupValidator.TryValidate(out var startupMessage))
        {
            MessageBox.Show(startupMessage, UiText.L("TitleApplicationError"), MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown();
            return;
        }

        StartupDiagnostics.LogStartupSnapshot();

        try
        {
            AppServices.Initialize();
        }
        catch (DatabaseCompatibilityException ex)
        {
            ExceptionReporter.ReportHandled(ex, "Startup blocked by schema compatibility.");
            MessageBox.Show(
                UiText.Format("MsgOutdatedAppVersion", ex.ActualSchemaVersion, ex.SupportedSchemaVersion),
                UiText.L("TitleApplicationError"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
            return;
        }
        catch (Exception ex)
        {
            ExceptionReporter.ReportHandled(ex, "Application startup failed");
            MessageBox.Show(
                UiText.L("MsgStartupValidationFailed", "The app could not start safely."),
                UiText.L("TitleApplicationError"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        if (!LicenseService.EnsureActivated())
        {
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        mainWindow.WindowState = WindowState.Maximized;
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _startupGuard?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ExceptionReporter.Report(e.Exception, "Unhandled UI exception");
        e.Handled = true;
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            ExceptionReporter.Report(ex, "Unhandled application exception");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ExceptionReporter.Report(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}
