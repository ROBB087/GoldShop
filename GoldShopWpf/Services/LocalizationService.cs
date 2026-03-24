using System.Globalization;
using System.Linq;
using System.Windows;

namespace GoldShopWpf.Services;

public static class LocalizationService
{
    private const string ArPath = "Resources/Strings.ar.xaml";
    private const string EnPath = "Resources/Strings.en.xaml";

    public static string CurrentLanguage { get; private set; } = "ar";

    public static void SetLanguage(string code)
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri(code == "en" ? EnPath : ArPath, UriKind.Relative)
        };

        var appResources = Application.Current.Resources.MergedDictionaries;
        var existing = appResources.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.EndsWith(ArPath, System.StringComparison.OrdinalIgnoreCase) ||
             d.Source.OriginalString.EndsWith(EnPath, System.StringComparison.OrdinalIgnoreCase)));

        if (existing != null)
        {
            var index = appResources.IndexOf(existing);
            appResources[index] = dict;
        }
        else
        {
            appResources.Add(dict);
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
