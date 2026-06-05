#if NET8_0_OR_GREATER
#nullable enable
using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AlinasRailTools.Shared.Heightmap;

/// <summary>
/// Core tile generator for creating game heightmap PNG tiles.
/// Handles encoding heightmaps to the per-tile blue channel format.
/// </summary>
public class TileGenerator
{
    /// <summary>
    /// Game tile resolution (must be 513x513 for the game).
    /// </summary>
    public const int TILE_RESOLUTION = 513;

    /// <summary>
    /// Encode a heightmap to PNG format with per-tile blue channel encoding.
    /// Uses double precision throughout to maintain accuracy.
    /// </summary>
    /// <param name="heights">Heightmap array [y, x] in meters</param>
    /// <returns>PNG byte array</returns>
    public static byte[] EncodeToPng(double[,] heights)
    {
        int height = heights.GetLength(0);
        int width = heights.GetLength(1);

        if (width != TILE_RESOLUTION || height != TILE_RESOLUTION)
        {
            throw new ArgumentException($"Heightmap must be {TILE_RESOLUTION}x{TILE_RESOLUTION}, got {width}x{height}");
        }

        // Calculate tile's blue channel value from minimum height
        // Using Floor on minimum ensures all pixels fit within [base, base+1000] range
        var (minHeight, _) = Sampling.GetMinMax(heights);
        byte tileBlue = HeightEncoding.CalculateTileBlueChannel(minHeight);

        // Create image and encode each pixel
        using var image = new Image<Rgba32>(width, height);

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    double pixelHeight = heights[y, x];

                    // Encode height relative to tile's base offset
                    var (r, g) = HeightEncoding.EncodeHeightRelativeToTile(pixelHeight, tileBlue);

                    // Alpha channel: store vegetation and water masks (for now, set to 0)
                    // In the future, this could be populated from additional data sources
                    byte alpha = 0;

                    row[x] = new Rgba32(r, g, tileBlue, alpha);
                }
            }
        });

        // Encode to PNG
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Encode and save a heightmap to a PNG file.
    /// </summary>
    /// <param name="heights">Heightmap array [y, x] in meters</param>
    /// <param name="outputPath">Output file path</param>
    public static void SaveTile(double[,] heights, string outputPath)
    {
        byte[] pngData = EncodeToPng(heights);

        // Ensure directory exists
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(outputPath, pngData);
    }

    /// <summary>
    /// Decode a heightmap from a PNG file.
    /// Useful for validation and testing.
    /// </summary>
    /// <param name="pngPath">Path to PNG file</param>
    /// <returns>Heightmap array [y, x] in meters</returns>
    public static double[,] LoadTile(string pngPath)
    {
        using var image = Image.Load<Rgba32>(pngPath);

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
                    var pixel = row[x];
                    heights[y, x] = HeightEncoding.DecodeHeight(pixel.R, pixel.G, pixel.B);
                }
            }
        });

        return heights;
    }

    /// <summary>
    /// Validate that a heightmap round-trips correctly through encoding/decoding.
    /// Returns statistics about precision errors.
    /// </summary>
    public static ValidationResult ValidateRoundTrip(double[,] originalHeights)
    {
        // Encode to PNG
        byte[] pngData = EncodeToPng(originalHeights);

        // Decode back
        using var ms = new MemoryStream(pngData);
        using var image = Image.Load<Rgba32>(ms);

        int width = originalHeights.GetLength(1);
        int height = originalHeights.GetLength(0);

        double maxError = 0.0;
        double totalError = 0.0;
        int pixelCount = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    double original = originalHeights[y, x];
                    var pixel = row[x];
                    double decoded = HeightEncoding.DecodeHeight(pixel.R, pixel.G, pixel.B);

                    double error = Math.Abs(decoded - original);
                    maxError = Math.Max(maxError, error);
                    totalError += error;
                    pixelCount++;
                }
            }
        });

        return new ValidationResult
        {
            MaxError = maxError,
            AverageError = totalError / pixelCount,
            TotalPixels = pixelCount
        };
    }
}

/// <summary>
/// Results from validating heightmap round-trip encoding/decoding.
/// </summary>
public class ValidationResult
{
    public double MaxError { get; set; }
    public double AverageError { get; set; }
    public int TotalPixels { get; set; }

    public bool IsAcceptable(double maxErrorThreshold = 0.02) // 2cm threshold
    {
        return MaxError <= maxErrorThreshold;
    }

    public override string ToString()
    {
        return $"Max Error: {MaxError:F4}m, Avg Error: {AverageError:F6}m, Pixels: {TotalPixels}";
    }
}
#endif
