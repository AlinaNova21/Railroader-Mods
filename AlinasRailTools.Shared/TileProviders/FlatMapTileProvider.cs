using System;
using System.IO;

#nullable enable

#if NET8_0_OR_GREATER
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace AlinasRailTools.Shared.TileProviders;

/// <summary>
/// Tile provider that generates flat (sea level) tiles within a bounded area.
/// Useful for testing or creating completely flat terrains.
/// </summary>
public class FlatMapTileProvider : ITileProvider
{
    private const float FLAT_HEIGHT = 0f; // Sea level
    private const int DEFAULT_MAX_TILE_COORD = 25; // 50x50 grid = 25km x 25km

    private readonly int _maxTileCoord;

    public string ProviderType => "flatmap";
    public string DisplayName => "FlatMap (Synthetic Flat Terrain)";

    /// <summary>
    /// Creates a FlatMap provider with default 50x50 tile bounds.
    /// </summary>
    public FlatMapTileProvider() : this(DEFAULT_MAX_TILE_COORD)
    {
    }

    /// <summary>
    /// Creates a FlatMap provider with custom bounds.
    /// </summary>
    /// <param name="maxTileCoord">Maximum tile coordinate (bounds are ±maxTileCoord)</param>
    public FlatMapTileProvider(int maxTileCoord)
    {
        _maxTileCoord = maxTileCoord;
    }

    public bool CanProvideTile(int tileX, int tileY)
    {
        // Tiles are bounded to ±_maxTileCoord
        return Math.Abs(tileX) <= _maxTileCoord && Math.Abs(tileY) <= _maxTileCoord;
    }

    public float[] GenerateHeightmap(int tileX, int tileY, int resolution)
    {
        if (!CanProvideTile(tileX, tileY))
            throw new ArgumentOutOfRangeException($"Tile ({tileX},{tileY}) is outside FlatMap bounds (±{_maxTileCoord})");

        // Create a flat array of sea-level heights
        int totalElements = resolution * resolution;
        float[] heightmap = new float[totalElements];

        // Fill with flat height (0m = sea level)
        for (int i = 0; i < totalElements; i++)
        {
            heightmap[i] = FLAT_HEIGHT;
        }

        return heightmap;
    }

    public object? GetMetadata()
    {
        return new
        {
            Type = ProviderType,
            BoundedArea = $"±{_maxTileCoord} tiles",
            TotalTiles = (_maxTileCoord * 2 + 1) * (_maxTileCoord * 2 + 1),
            Height = FLAT_HEIGHT,
            Description = "Synthetic flat terrain at sea level"
        };
    }
}
