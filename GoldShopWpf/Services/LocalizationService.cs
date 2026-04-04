using System.Globalization;
using System.Linq;
using System.Windows;

namespace GoldShopWpf.Services;

public static class LocalizationService
{
    private const string ArPath = "Resources/Strings.ar.xaml";
    private const string ArOverridesPath = "Resources/Strings.ar.overrides.xaml";
    private const string EnPath = "Resources/Strings.en.xaml";

    public static string CurrentLanguage { get; private set; } = "en";

    public static void SetLanguage(string code)
    {
        var appResources = Application.Current.Resources.MergedDictionaries;
        var existing = appResources
            .Where(d => d.Source != null &&
                        (d.Source.OriginalString.EndsWith(ArPath, System.StringComparison.OrdinalIgnoreCase) ||
                         d.Source.OriginalString.EndsWith(ArOverridesPath, System.StringComparison.OrdinalIgnoreCase) ||
                         d.Source.OriginalString.EndsWith(EnPath, System.StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var dictionary in existing)
        {
            appResources.Remove(dictionary);
        }

        if (code == "en")
        {
            appResources.Add(new ResourceDictionary { Source = new Uri(EnPath, UriKind.Relative) });
        }
        else
        {
            appResources.Add(new ResourceDictionary { Source = new Uri(ArPath, UriKind.Relative) });
            appResources.Add(new ResourceDictionary { Source = new Uri(ArOverridesPath, UriKind.Relative) });
        }

        if (code == "en")
        {
            Application.Current.Resources["AppFlowDirection"] = FlowDirection.LeftToRight;
            Application.Current.Resources["AppFontFamily"] = new System.Windows.Media.FontFamily("Segoe UI");
            var culture = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CurrentLanguage = "en";
        }
        else
        {
            Application.Current.Resources["AppFlowDirection"] = FlowDirection.RightToLeft;
            Application.Current.Resources["AppFontFamily"] = new System.Windows.Media.FontFamily("Tahoma");
            var numberCulture = new CultureInfo("en-US");
            var uiCulture = new CultureInfo("ar-EG");
            CultureInfo.CurrentCulture = numberCulture;
            CultureInfo.CurrentUICulture = uiCulture;
            CultureInfo.DefaultThreadCurrentCulture = numberCulture;
            CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
            CurrentLanguage = "ar";
        }
    }
}
