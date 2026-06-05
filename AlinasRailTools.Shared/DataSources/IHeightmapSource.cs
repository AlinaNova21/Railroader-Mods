using System.Threading.Tasks;
using AlinasRailTools.Shared.Heightmap;

namespace AlinasRailTools.Shared.DataSources;

/// <summary>
/// Interface for heightmap data sources.
/// Implementations provide height data from various sources (MapBox, local files, procedural, etc.)
/// </summary>
public interface IHeightmapSource
{
    /// <summary>
    /// Get the name of this data source (e.g., "mapbox", "synthetic", "geotiff").
    /// Used for metadata tracking and logging.
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Generate a heightmap for a specific game tile.
    /// </summary>
    /// <param name="gameTile">Game tile coordinates</param>
    /// <param name="resolution">Output resolution (typically 513x513)</param>
    /// <param name="centerLat">Center latitude of the game world origin</param>
    /// <param name="centerLng">Center longitude of the game world origin</param>
    /// <param name="metersPerTile">Size of each game tile in meters</param>
    /// <returns>Heightmap array [y, x] in meters</returns>
    Task<double[,]> GenerateHeightmapAsync(
        GameTileCoord gameTile,
        int resolution,
        double centerLat,
        double centerLng,
        double metersPerTile);

    /// <summary>
    /// Get metadata about this data source for inclusion in map JSON files.
    /// Should include any relevant configuration (API version, source files, parameters, etc.)
    /// </summary>
    object GetMetadata();
}
