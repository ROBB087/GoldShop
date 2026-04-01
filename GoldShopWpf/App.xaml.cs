using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GoldShopWpf.Services;
using System.Windows.Threading;

namespace GoldShopWpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);
        AppServices.Initialize();
        LocalizationService.SetLanguage("ar");
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

    private void OnDatePickerPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.DatePicker picker)
        {
            if (!picker.IsDropDownOpen)
            {
                Dispatcher.BeginInvoke(new Action(() => picker.IsDropDownOpen = true), System.Windows.Threading.DispatcherPriority.Input);
                e.Handled = true;
            }
        }
    }

    private void OnDatePickerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker picker)
        {
            Dispatcher.BeginInvoke(() => UpdateDatePickerText(picker), DispatcherPriority.Input);
        }
    }

    private void OnDatePickerSelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DatePicker picker)
        {
            Dispatcher.BeginInvoke(() => UpdateDatePickerText(picker), DispatcherPriority.Input);
        }
    }

    private static void UpdateDatePickerText(DatePicker picker)
    {
        picker.ApplyTemplate();
        if (picker.Template.FindName("PART_TextBox", picker) is DatePickerTextBox textBox)
        {
            textBox.Text = picker.SelectedDate?.ToString("yyyy/MM/dd") ?? string.Empty;
        }
    }
}
