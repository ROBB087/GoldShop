using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class CacheService
{
    private readonly object _suppliersLock = new();
    private readonly object _pricingLock = new();
    private readonly object _summariesLock = new();
    private List<Supplier>? _suppliers;
    private PricingSettings? _pricingSettings;
    private Dictionary<int, TraderSummarySnapshot>? _traderSummaries;

    public void PreloadSuppliers(IEnumerable<Supplier> suppliers)
    {
        lock (_suppliersLock)
        {
            _suppliers = suppliers.Select(CloneSupplier).ToList();
        }
    }

    public List<Supplier> GetSuppliers(Func<List<Supplier>> loader)
    {
        lock (_suppliersLock)
        {
            if (_suppliers != null)
            {
                return _suppliers.Select(CloneSupplier).ToList();
            }
        }

        var loaded = loader();
        PreloadSuppliers(loaded);
        return loaded.Select(CloneSupplier).ToList();
    }

    public void SetSupplier(Supplier supplier)
    {
        lock (_suppliersLock)
        {
            _suppliers ??= [];
            var existingIndex = _suppliers.FindIndex(item => item.Id == supplier.Id);
            if (existingIndex >= 0)
            {
                _suppliers[existingIndex] = CloneSupplier(supplier);
            }
            else
            {
                _suppliers.Add(CloneSupplier(supplier));
                _suppliers.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase));
            }
        }
    }

    public void RemoveSupplier(int supplierId)
    {
        lock (_suppliersLock)
        {
            if (_suppliers == null)
            {
                return;
            }

            _suppliers.RemoveAll(supplier => supplier.Id == supplierId);
        }
    }

    public void PreloadPricingSettings(PricingSettings settings)
    {
        lock (_pricingLock)
        {
            _pricingSettings = ClonePricing(settings);
        }
    }

    public PricingSettings GetPricingSettings(Func<PricingSettings> loader)
    {
        lock (_pricingLock)
        {
            if (_pricingSettings != null)
            {
                return ClonePricing(_pricingSettings);
            }
        }

        var loaded = loader();
        PreloadPricingSettings(loaded);
        return ClonePricing(loaded);
    }

    public void SetPricingSettings(PricingSettings settings) => PreloadPricingSettings(settings);

    public void PreloadTraderSummaries(Dictionary<int, TraderSummarySnapshot> summaries)
    {
        lock (_summariesLock)
        {
            _traderSummaries = summaries.ToDictionary(entry => entry.Key, entry => CloneSummary(entry.Value));
        }
    }

    public Dictionary<int, TraderSummarySnapshot> GetTraderSummaries(Func<Dictionary<int, TraderSummarySnapshot>> loader)
    {
        lock (_summariesLock)
        {
            if (_traderSummaries != null)
            {
                return CloneSummaries(_traderSummaries);
            }
        }

        var loaded = loader();
        PreloadTraderSummaries(loaded);
        return CloneSummaries(loaded);
    }

    public TraderSummarySnapshot? GetTraderSummary(int traderId, Func<TraderSummarySnapshot?> loader)
    {
        lock (_summariesLock)
        {
            if (_traderSummaries != null && _traderSummaries.TryGetValue(traderId, out var cached))
            {
                return CloneSummary(cached);
            }
        }

        var loaded = loader();
        if (loaded == null)
        {
            return null;
        }

        SetTraderSummary(loaded);
        return CloneSummary(loaded);
    }

    public void SetTraderSummary(TraderSummarySnapshot summary)
    {
        lock (_summariesLock)
        {
            _traderSummaries ??= new Dictionary<int, TraderSummarySnapshot>();
            _traderSummaries[summary.TraderId] = CloneSummary(summary);
        }
    }

    public void RemoveTraderSummary(int traderId)
    {
        lock (_summariesLock)
        {
            _traderSummaries?.Remove(traderId);
        }
    }

    private static Dictionary<int, TraderSummarySnapshot> CloneSummaries(Dictionary<int, TraderSummarySnapshot> source)
        => source.ToDictionary(entry => entry.Key, entry => CloneSummary(entry.Value));

    private static Supplier CloneSupplier(Supplier supplier)
    {
        return new Supplier
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Phone = supplier.Phone,
            WorkerName = supplier.WorkerName,
            WorkerPhone = supplier.WorkerPhone,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt
        };
    }

    private static PricingSettings ClonePricing(PricingSettings settings)
    {
        return new PricingSettings
        {
            Id = settings.Id,
            DefaultManufacturingPerGram = settings.DefaultManufacturingPerGram,
            DefaultManufacturingPerGram24 = settings.DefaultManufacturingPerGram24,
            DefaultImprovementPerGram = settings.DefaultImprovementPerGram,
            CreatedAt = settings.CreatedAt
        };
    }

    private static TraderSummarySnapshot CloneSummary(TraderSummarySnapshot summary)
    {
        return new TraderSummarySnapshot
        {
            TraderId = summary.TraderId,
            TotalEquivalent21 = summary.TotalEquivalent21,
            TotalManufacturing = summary.TotalManufacturing,
            TotalImprovement = summary.TotalImprovement,
            ManufacturingAdjustments = summary.ManufacturingAdjustments,
            ImprovementAdjustments = summary.ImprovementAdjustments,
            ManufacturingDiscounts = summary.ManufacturingDiscounts,
            ImprovementDiscounts = summary.ImprovementDiscounts,
            LastUpdated = summary.LastUpdated
        };
    }
}
