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
            ToastService.ShowWarning(UiText.L("MsgManufacturingInvalid"));
            return;
        }

        if (!TryParse(DefaultImprovementPerGram, out var improvement))
        {
            ToastService.ShowWarning(UiText.L("MsgImprovementInvalid"));
            return;
        }

        try
        {
            AppServices.PricingSettingsService.Save(manufacturing, improvement);
            Load();
            ToastService.ShowSuccess(UiText.L("MsgPricingSettingsSaved"));
        }
        catch (ArgumentException ex)
        {
            ToastService.ShowWarning(UiText.LocalizeException(ex.Message));
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
