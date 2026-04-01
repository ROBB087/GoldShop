using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class PricingSettingsService
{
    private readonly PricingSettingsRepository _pricingSettingsRepository;

    public PricingSettingsService(PricingSettingsRepository pricingSettingsRepository)
    {
        _pricingSettingsRepository = pricingSettingsRepository;
    }

    public PricingSettings GetLatest()
    {
        return _pricingSettingsRepository.GetLatest() ?? new PricingSettings
        {
            DefaultManufacturingPerGram = 0,
            DefaultImprovementPerGram = 0,
            CreatedAt = DateTime.Now
        };
    }

    public void Save(decimal defaultManufacturingPerGram, decimal defaultImprovementPerGram)
    {
        if (defaultManufacturingPerGram < 0)
        {
            throw new ArgumentException("Default manufacturing value must be zero or greater.", nameof(defaultManufacturingPerGram));
        }

        if (defaultImprovementPerGram < 0)
        {
            throw new ArgumentException("Default refining value must be zero or greater.", nameof(defaultImprovementPerGram));
        }

        _pricingSettingsRepository.Add(new PricingSettings
        {
            DefaultManufacturingPerGram = decimal.Round(defaultManufacturingPerGram, 4, MidpointRounding.AwayFromZero),
            DefaultImprovementPerGram = decimal.Round(defaultImprovementPerGram, 4, MidpointRounding.AwayFromZero),
            CreatedAt = DateTime.Now
        });
    }
}
