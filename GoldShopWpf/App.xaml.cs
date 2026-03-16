using System.Windows;
using GoldShopWpf.Services;

namespace GoldShopWpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppServices.Initialize();
        LocalizationService.SetLanguage("ar");
    }
}
