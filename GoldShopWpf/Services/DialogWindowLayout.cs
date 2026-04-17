using System.Windows;

namespace GoldShopWpf.Services;

public static class DialogWindowLayout
{
    public static void Apply(Window window)
    {
        var mainWindow = Application.Current?.MainWindow;
        if (mainWindow != null && !ReferenceEquals(mainWindow, window) && window.Owner == null)
        {
            window.Owner = mainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else if (window.WindowStartupLocation != WindowStartupLocation.CenterOwner)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        var workArea = SystemParameters.WorkArea;
        window.MaxWidth = Math.Max(window.MinWidth, workArea.Width - 40);
        window.MaxHeight = Math.Max(window.MinHeight, workArea.Height - 40);

        if (window.Width > window.MaxWidth)
        {
            window.Width = window.MaxWidth;
        }

        if (window.Height > window.MaxHeight)
        {
            window.Height = window.MaxHeight;
        }
    }
}
