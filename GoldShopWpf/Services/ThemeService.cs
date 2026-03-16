namespace GoldShopWpf.Services;

public static class ThemeService
{
    private static bool _isDark;

    public static bool IsDark => _isDark;

    public static void ToggleTheme()
    {
        _isDark = !_isDark;
        var resources = System.Windows.Application.Current.Resources;

        if (_isDark)
        {
            resources["AppBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            resources["CardBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(43, 43, 43));
            resources["TextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
            resources["MutedTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
            resources["SidebarBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            resources["SidebarTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
            resources["GridBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(43, 43, 43));
            resources["GridAltRowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));
            resources["GridHeaderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 58));
            resources["GridLineBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(68, 68, 68));
            resources["InputBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
            resources["InputBorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85));
            resources["GhostButtonBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
            resources["GhostButtonHoverBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(72, 72, 72));
            resources["GoldValueBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0));
        }
        else
        {
            resources["AppBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            resources["CardBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            resources["TextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            resources["MutedTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102));
            resources["SidebarBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            resources["SidebarTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 241, 232));
            resources["GridBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            resources["GridAltRowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(247, 247, 247));
            resources["GridHeaderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 229, 229));
            resources["GridLineBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221));
            resources["InputBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            resources["InputBorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
            resources["GhostButtonBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 231, 213));
            resources["GhostButtonHoverBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 215, 184));
            resources["GoldValueBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0));
        }
    }
}
