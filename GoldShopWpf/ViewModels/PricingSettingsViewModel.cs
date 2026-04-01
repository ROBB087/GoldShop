using System.Globalization;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class PricingSettingsViewModel : ViewModelBase
{
    private string _defaultManufacturingPerGram = string.Empty;
    private string _defaultImprovementPerGram = string.Empty;

    public string DefaultManufacturingPerGram
    {
        get => _defaultManufacturingPerGram;
        set => SetProperty(ref _defaultManufacturingPerGram, value);
    }

    public string DefaultImprovementPerGram
    {
        get => _defaultImprovementPerGram;
        set => SetProperty(ref _defaultImprovementPerGram, value);
    }

    public RelayCommand SaveCommand { get; }

    public PricingSettingsViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save());
        Load();
    }

    public void Load()
    {
        var settings = AppServices.PricingSettingsService.GetLatest();
        DefaultManufacturingPerGram = settings.DefaultManufacturingPerGram.ToString("0.####", CultureInfo.CurrentCulture);
        DefaultImprovementPerGram = settings.DefaultImprovementPerGram.ToString("0.####", CultureInfo.CurrentCulture);
    }

    private void Save()
    {
        if (!TryParse(DefaultManufacturingPerGram, out var manufacturing))
        {
            System.Windows.MessageBox.Show(UiText.L("MsgManufacturingInvalid"), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (!TryParse(DefaultImprovementPerGram, out var improvement))
        {
            System.Windows.MessageBox.Show(UiText.L("MsgImprovementInvalid"), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            AppServices.PricingSettingsService.Save(manufacturing, improvement);
            Load();
            System.Windows.MessageBox.Show(UiText.L("MsgPricingSettingsSaved"), UiText.L("NavPricingSettings"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (ArgumentException ex)
        {
            System.Windows.MessageBox.Show(UiText.LocalizeException(ex.Message), UiText.L("TitleValidation"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private static bool TryParse(string text, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0m;
            return true;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
    }
}
