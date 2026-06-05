using System;
using System.Collections.Generic;
using System.Linq;

namespace AlinasRailTools.TileGenerator.TileProviders;

/// <summary>
/// Registry for managing available tile providers.
/// </summary>
public class TileProviderRegistry
{
    private readonly Dictionary<string, ITileProvider> _providers = new();

    /// <summary>
    /// Register a tile provider.
    /// </summary>
    public void RegisterProvider(ITileProvider provider)
    {
        _providers[provider.Id] = provider;
    }

    /// <summary>
    /// Get a provider by ID.
    /// </summary>
    public ITileProvider? GetProvider(string id)
    {
        return _providers.TryGetValue(id, out var provider) ? provider : null;
    }

    /// <summary>
    /// Get all registered provider IDs.
    /// </summary>
    public IEnumerable<string> GetProviderIds()
    {
        return _providers.Keys;
    }

    /// <summary>
    /// Get all registered providers.
    /// </summary>
    public IEnumerable<ITileProvider> GetProviders()
    {
        return _providers.Values;
    }

    /// <summary>
    /// Get provider metadata for the client.
    /// </summary>
    public object[] GetProviderMetadata()
    {
        return _providers.Values
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                requiresApiKey = p.RequiresApiKey
            })
            .ToArray();
    }

    /// <summary>
    /// Auto-configure providers based on available API keys.
    /// </summary>
    public static TileProviderRegistry CreateDefault(string cacheBaseDir, string? mapBoxApiKey)
    {
        var registry = new TileProviderRegistry();

        // Register MapBox providers if API key is available
        if (!string.IsNullOrEmpty(mapBoxApiKey))
        {
            registry.RegisterProvider(new MapBoxTerrainProvider(cacheBaseDir, mapBoxApiKey, "grayscale"));
            registry.RegisterProvider(new MapBoxTerrainProvider(cacheBaseDir, mapBoxApiKey, "color"));
            registry.RegisterProvider(new MapBoxStreetsProvider(cacheBaseDir, mapBoxApiKey));
            registry.RegisterProvider(new MapBoxSatelliteProvider(cacheBaseDir, mapBoxApiKey));
            registry.RegisterProvider(new MapBoxOutdoorsProvider(cacheBaseDir, mapBoxApiKey));
        }

        // Always register free providers
        registry.RegisterProvider(new OpenStreetMapProvider(cacheBaseDir));
        registry.RegisterProvider(new OpenTopoMapProvider(cacheBaseDir));

        return registry;
    }
}
