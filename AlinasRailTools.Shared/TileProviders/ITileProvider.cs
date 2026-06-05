#nullable enable

namespace AlinasRailTools.Shared.TileProviders;

/// <summary>
/// Interface for tile providers that can generate map tiles on-demand.
/// This is a game-agnostic interface - implementations should not reference Unity or game types.
/// Uses simple types (float[], int) for portability across different environments.
/// </summary>
public interface ITileProvider
{
    /// <summary>
    /// Unique identifier for this provider type (e.g., "vanilla", "flatmap", "procedural").
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Display name for this provider.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Checks if this provider can provide a tile at the given coordinates.
    /// </summary>
    /// <param name="tileX">Tile X coordinate</param>
    /// <param name="tileY">Tile Y coordinate</param>
    /// <returns>True if the provider can generate/provide this tile</returns>
    bool CanProvideTile(int tileX, int tileY);

    /// <summary>
    /// Generates a heightmap for the tile at the given coordinates.
    /// Heightmap is a flat array of floats in row-major order (y * resolution + x).
    /// </summary>
    /// <param name="tileX">Tile X coordinate</param>
    /// <param name="tileY">Tile Y coordinate</param>
    /// <param name="resolution">Resolution of the heightmap (typically 513x513 = 263169 elements)</param>
    /// <returns>Flat array of height values in meters</returns>
    float[] GenerateHeightmap(int tileX, int tileY, int resolution);

    /// <summary>
    /// Gets provider-specific metadata (e.g., source information, version, etc.).
    /// </summary>
    /// <returns>Metadata object or null if not applicable</returns>
    object? GetMetadata();
}
