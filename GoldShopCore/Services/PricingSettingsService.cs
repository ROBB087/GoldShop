using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class PricingSettingsService
{
    private readonly PricingSettingsRepository _pricingSettingsRepository;
    private readonly AuditService _auditService;
    private readonly CacheService _cacheService;

    public PricingSettingsService(PricingSettingsRepository pricingSettingsRepository, AuditService auditService, CacheService cacheService)
    {
        _pricingSettingsRepository = pricingSettingsRepository;
        _auditService = auditService;
        _cacheService = cacheService;
    }

    public PricingSettings GetLatest()
    {
        return _cacheService.GetPricingSettings(() => _pricingSettingsRepository.GetLatest() ?? new PricingSettings
        {
            DefaultManufacturingPerGram = 0,
            DefaultImprovementPerGram = 0,
            CreatedAt = DateTime.Now
        });
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

        var latest = _pricingSettingsRepository.GetLatest();
        var settings = new PricingSettings
        {
            DefaultManufacturingPerGram = decimal.Round(defaultManufacturingPerGram, 4, MidpointRounding.AwayFromZero),
            DefaultImprovementPerGram = decimal.Round(defaultImprovementPerGram, 4, MidpointRounding.AwayFromZero),
            CreatedAt = DateTime.Now
        };

        settings.Id = _pricingSettingsRepository.Add(settings);
        _cacheService.SetPricingSettings(settings);
        _auditService.Log("PricingSettings", settings.Id, "Create", latest, settings);
    }
}
