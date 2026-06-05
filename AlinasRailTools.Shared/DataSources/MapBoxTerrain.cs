#if NET8_0_OR_GREATER
using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AlinasRailTools.Shared.DataSources;

/// <summary>
/// MapBox Terrain-RGB decoder.
/// Formula: height = -10000 + ((R * 256 * 256 + G * 256 + B) * 0.1)
/// </summary>
public static class MapBoxTerrain
{
    /// <summary>
    /// Decode height from a MapBox terrain-RGB pixel.
    /// </summary>
    public static double DecodeHeight(Rgb24 pixel)
    {
        uint r = pixel.R;
        uint g = pixel.G;
        uint b = pixel.B;

        uint value = r * 256 * 256 + g * 256 + b;
        return -10000.0 + (value * 0.1);
    }

    /// <summary>
    /// Decode all heights from a MapBox terrain-RGB image.
    /// Returns a 2D array [y, x] of heights in meters.
    /// Uses double precision for accuracy.
    /// </summary>
    public static double[,] DecodeImage(Image<Rgb24> image)
    {
        int width = image.Width;
        int height = image.Height;
        var heights = new double[height, width];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    heights[y, x] = DecodeHeight(row[x]);
                }
            }
        });

        return heights;
    }
}

/// <summary>
/// Merged tile array - stitches multiple MapBox tiles into a single heightmap.
/// </summary>
public class MergedTileArray
{
    public double[,] Heights { get; }
    public int Width { get; }
    public int Height { get; }
    public int TileSize { get; }
    public uint TileCountX { get; }
    public uint TileCountY { get; }
    public uint MinTileX { get; }
    public uint MinTileY { get; }

    private MergedTileArray(double[,] heights, int width, int height, int tileSize,
        uint tileCountX, uint tileCountY, uint minTileX, uint minTileY)
    {
        Heights = heights;
        Width = width;
        Height = height;
        TileSize = tileSize;
        TileCountX = tileCountX;
        TileCountY = tileCountY;
        MinTileX = minTileX;
        MinTileY = minTileY;
    }

    /// <summary>
    /// Create a merged array from a grid of MapBox tiles.
    /// </summary>
    /// <param name="tiles">Dictionary of (x, y) -> Image mappings</param>
    /// <param name="minTileX">Minimum tile X coordinate</param>
    /// <param name="minTileY">Minimum tile Y coordinate</param>
    /// <param name="tileCountX">Number of tiles in X direction</param>
    /// <param name="tileCountY">Number of tiles in Y direction</param>
    public static MergedTileArray FromTiles(
        System.Collections.Generic.Dictionary<(uint x, uint y), Image<Rgb24>> tiles,
        uint minTileX,
        uint minTileY,
        uint tileCountX,
        uint tileCountY)
    {
        // Determine tile size from first tile (typically 512x512)
        int tileSize = 512;
        foreach (var img in tiles.Values)
        {
            tileSize = img.Width;
            break;
        }

        int width = tileSize * (int)tileCountX;
        int height = tileSize * (int)tileCountY;
        var heights = new double[height, width];

        // Fill merged array with decoded heights from each tile
        foreach (var kvp in tiles)
        {
            var (tileX, tileY) = kvp.Key;
            var img = kvp.Value;

            int offsetX = (int)(tileX - minTileX) * tileSize;
            int offsetY = (int)(tileY - minTileY) * tileSize;

            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < img.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < img.Width; x++)
                    {
                        int mergedX = offsetX + x;
                        int mergedY = offsetY + y;

                        if (mergedX < width && mergedY < height)
                        {
                            heights[mergedY, mergedX] = MapBoxTerrain.DecodeHeight(row[x]);
                        }
                    }
                }
            });
        }

        return new MergedTileArray(heights, width, height, tileSize, tileCountX, tileCountY, minTileX, minTileY);
    }

    /// <summary>
    /// Sample height at fractional pixel position using bilinear interpolation.
    /// </summary>
    public double Sample(double x, double y)
    {
        // Clamp to valid range
        x = Math.Clamp(x, 0.0, Width - 1);
        y = Math.Clamp(y, 0.0, Height - 1);

        int x0 = (int)Math.Floor(x);
        int y0 = (int)Math.Floor(y);
        int x1 = Math.Min(x0 + 1, Width - 1);
        int y1 = Math.Min(y0 + 1, Height - 1);

        double dx = x - x0;
        double dy = y - y0;

        // Bilinear interpolation
        double h00 = Heights[y0, x0];
        double h10 = Heights[y0, x1];
        double h01 = Heights[y1, x0];
        double h11 = Heights[y1, x1];

        double h0 = h00 * (1.0 - dx) + h10 * dx;
        double h1 = h01 * (1.0 - dx) + h11 * dx;
        return h0 * (1.0 - dy) + h1 * dy;
    }
}
#endif
