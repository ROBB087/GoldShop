using System.Windows;
using System.Windows.Threading;
using GoldShopWpf.Services;

namespace GoldShopWpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);
        LocalizationService.SetLanguage("ar");

        if (!LicenseService.EnsureActivated())
        {
            Shutdown();
            return;
        }

        AppServices.Initialize();

        var mainWindow = new MainWindow();
        mainWindow.WindowState = WindowState.Maximized;
        MainWindow = mainWindow;
        mainWindow.Show();
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
