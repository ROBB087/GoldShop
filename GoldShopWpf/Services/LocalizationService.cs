using System.Globalization;
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

        // ensure first dictionary is our strings
        if (appResources.Count > 0)
        {
            appResources[0] = dict;
        }
        else
        {
            appResources.Add(dict);
        }

        if (code == "en")
        {
            Application.Current.Resources["AppFlowDirection"] = FlowDirection.LeftToRight;
            Application.Current.Resources["AppFontFamily"] = new System.Windows.Media.FontFamily("Segoe UI");
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            CurrentLanguage = "en";
        }
        else
        {
            Application.Current.Resources["AppFlowDirection"] = FlowDirection.RightToLeft;
            Application.Current.Resources["AppFontFamily"] = new System.Windows.Media.FontFamily("Tahoma");
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("ar-EG");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("ar-EG");
            CurrentLanguage = "ar";
        }
    }
}
