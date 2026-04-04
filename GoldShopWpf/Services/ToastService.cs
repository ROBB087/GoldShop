using System.Collections.ObjectModel;
using System.Windows.Threading;
using GoldShopCore.Services;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Services;

public static class ToastService
{
    public static ObservableCollection<ToastMessageViewModel> Messages { get; } = new();

    public static void ShowSuccess(string message) => Show(message, ToastKind.Success);

    public static void ShowWarning(string message) => Show(message, ToastKind.Warning);

    public static void ShowError(string message) => Show(message, ToastKind.Error);

    private static void Show(string message, ToastKind kind)
    {
        try
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            dispatcher.Invoke(() =>
            {
                var toast = new ToastMessageViewModel(message, kind);
                Messages.Add(toast);

                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(kind == ToastKind.Error ? 5 : 3.5)
                };

                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    Messages.Remove(toast);
                };

                timer.Start();
            });
        }
        catch (Exception ex)
        {
            FileLogService.LogError("Toast notification failed", ex);
        }
    }
}
