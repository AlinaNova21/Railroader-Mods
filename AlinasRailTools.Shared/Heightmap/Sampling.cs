using System;

namespace AlinasRailTools.Shared.Heightmap;

/// <summary>
/// Utilities for sampling and resampling heightmap data with high precision.
/// </summary>
public static class Sampling
{
    /// <summary>
    /// Sample a heightmap using bilinear interpolation.
    /// Uses double precision throughout to maintain accuracy.
    /// </summary>
    /// <param name="heights">2D heightmap array [y, x]</param>
    /// <param name="x">X coordinate (can be fractional)</param>
    /// <param name="y">Y coordinate (can be fractional)</param>
    /// <returns>Interpolated height value</returns>
    public static double BilinearSample(double[,] heights, double x, double y)
    {
        int width = heights.GetLength(1);
        int height = heights.GetLength(0);

        // Get integer coordinates
        int x0 = (int)Math.Floor(x);
        int y0 = (int)Math.Floor(y);
        int x1 = Math.Min(x0 + 1, width - 1);
        int y1 = Math.Min(y0 + 1, height - 1);

        // Clamp to bounds
        x0 = MathHelpers.Clamp(x0, 0, width - 1);
        y0 = MathHelpers.Clamp(y0, 0, height - 1);

        // Get fractional parts
        double dx = x - x0;
        double dy = y - y0;

        // Sample four corners
        double h00 = heights[y0, x0];
        double h10 = heights[y0, x1];
        double h01 = heights[y1, x0];
        double h11 = heights[y1, x1];

        // Interpolate
        double h0 = h00 * (1.0 - dx) + h10 * dx;
        double h1 = h01 * (1.0 - dx) + h11 * dx;
        return h0 * (1.0 - dy) + h1 * dy;
    }

    /// <summary>
    /// Resample a source heightmap to a target size using bilinear interpolation.
    /// </summary>
    /// <param name="source">Source heightmap [y, x]</param>
    /// <param name="targetWidth">Target width</param>
    /// <param name="targetHeight">Target height</param>
    /// <returns>Resampled heightmap</returns>
    public static double[,] Resample(double[,] source, int targetWidth, int targetHeight)
    {
        int sourceWidth = source.GetLength(1);
        int sourceHeight = source.GetLength(0);

        double[,] result = new double[targetHeight, targetWidth];

        double scaleX = (double)(sourceWidth - 1) / (targetWidth - 1);
        double scaleY = (double)(sourceHeight - 1) / (targetHeight - 1);

        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                double sourceX = x * scaleX;
                double sourceY = y * scaleY;
                result[y, x] = BilinearSample(source, sourceX, sourceY);
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate the average height across all pixels in a heightmap.
    /// </summary>
    public static double CalculateAverage(double[,] heights)
    {
        int width = heights.GetLength(1);
        int height = heights.GetLength(0);

        double sum = 0.0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                sum += heights[y, x];
            }
        }

        return sum / (width * height);
    }

    /// <summary>
    /// Find the minimum and maximum heights in a heightmap.
    /// </summary>
    public static (double min, double max) GetMinMax(double[,] heights)
    {
        int width = heights.GetLength(1);
        int height = heights.GetLength(0);

        double min = double.MaxValue;
        double max = double.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double h = heights[y, x];
                if (h < min) min = h;
                if (h > max) max = h;
            }
        }

        return (min, max);
    }
}
