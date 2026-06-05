using System.Threading.Tasks;

namespace AlinasRailTools.TileGenerator.TileProviders;

/// <summary>
/// Interface for tile providers that can fetch map tiles from various sources.
/// </summary>
public interface ITileProvider
{
    /// <summary>
    /// Display name for this provider (e.g., "MapBox Streets").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Unique identifier for this provider (e.g., "mapbox-streets").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Cache directory name for this provider's tiles.
    /// </summary>
    string CacheDirectory { get; }

    /// <summary>
    /// Whether this provider requires an API key.
    /// </summary>
    bool RequiresApiKey { get; }

    /// <summary>
    /// Get a tile, using cache if available, otherwise downloading and caching it.
    /// </summary>
    Task<byte[]> GetTileAsync(byte z, uint x, uint y);
}
