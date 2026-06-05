using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using AlinasRailTools.Shared.DataSources;

namespace AlinasRailTools.TileGenerator.Api;

/// <summary>
/// Converts terrain-RGB tiles to visually displayable images for web interface.
/// </summary>
public static class VisualizationService
{
    /// <summary>
    /// Convert a terrain-RGB MapBox tile to grayscale based on elevation.
    /// </summary>
    public static Image<Rgba32> TerrainRgbToGrayscale(Image<Rgb24> terrainRgb, double minHeight = 0, double maxHeight = 3000)
    {
        var output = new Image<Rgba32>(terrainRgb.Width, terrainRgb.Height);

        terrainRgb.ProcessPixelRows(output, (sourceAccessor, destAccessor) =>
        {
            for (int y = 0; y < terrainRgb.Height; y++)
            {
                var sourceRow = sourceAccessor.GetRowSpan(y);
                var destRow = destAccessor.GetRowSpan(y);

                for (int x = 0; x < terrainRgb.Width; x++)
                {
                    var pixel = sourceRow[x];

                    // Decode terrain-RGB to height
                    double height = MapBoxTerrain.DecodeHeight(pixel);

                    // Normalize to 0-1 range
                    double normalized = (height - minHeight) / (maxHeight - minHeight);
                    normalized = Math.Clamp(normalized, 0, 1);

                    // Convert to grayscale (0-255)
                    byte gray = (byte)(normalized * 255);

                    destRow[x] = new Rgba32(gray, gray, gray, 255);
                }
            }
        });

        return output;
    }

    /// <summary>
    /// Convert a terrain-RGB MapBox tile to colored elevation map.
    /// Uses elevation-based color gradient: blue (low) -> green -> yellow -> red (high)
    /// </summary>
    public static Image<Rgba32> TerrainRgbToColorMap(Image<Rgb24> terrainRgb, double minHeight = 0, double maxHeight = 3000)
    {
        var output = new Image<Rgba32>(terrainRgb.Width, terrainRgb.Height);

        terrainRgb.ProcessPixelRows(output, (sourceAccessor, destAccessor) =>
        {
            for (int y = 0; y < terrainRgb.Height; y++)
            {
                var sourceRow = sourceAccessor.GetRowSpan(y);
                var destRow = destAccessor.GetRowSpan(y);

                for (int x = 0; x < terrainRgb.Width; x++)
                {
                    var pixel = sourceRow[x];

                    // Decode terrain-RGB to height
                    double height = MapBoxTerrain.DecodeHeight(pixel);

                    // Normalize to 0-1 range
                    double normalized = (height - minHeight) / (maxHeight - minHeight);
                    normalized = Math.Clamp(normalized, 0, 1);

                    // Apply color gradient
                    var color = GetElevationColor(normalized);
                    destRow[x] = color;
                }
            }
        });

        return output;
    }

    private static Rgba32 GetElevationColor(double t)
    {
        // Color gradient: blue -> cyan -> green -> yellow -> orange -> red
        if (t < 0.2)
        {
            // Blue to cyan
            double local = t / 0.2;
            return Lerp(new Rgba32(0, 0, 255, 255), new Rgba32(0, 255, 255, 255), local);
        }
        else if (t < 0.4)
        {
            // Cyan to green
            double local = (t - 0.2) / 0.2;
            return Lerp(new Rgba32(0, 255, 255, 255), new Rgba32(0, 255, 0, 255), local);
        }
        else if (t < 0.6)
        {
            // Green to yellow
            double local = (t - 0.4) / 0.2;
            return Lerp(new Rgba32(0, 255, 0, 255), new Rgba32(255, 255, 0, 255), local);
        }
        else if (t < 0.8)
        {
            // Yellow to orange
            double local = (t - 0.6) / 0.2;
            return Lerp(new Rgba32(255, 255, 0, 255), new Rgba32(255, 128, 0, 255), local);
        }
        else
        {
            // Orange to red
            double local = (t - 0.8) / 0.2;
            return Lerp(new Rgba32(255, 128, 0, 255), new Rgba32(255, 0, 0, 255), local);
        }
    }

    private static Rgba32 Lerp(Rgba32 a, Rgba32 b, double t)
    {
        return new Rgba32(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            255
        );
    }
}
