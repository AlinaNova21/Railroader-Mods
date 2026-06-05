using System;
using System.Threading.Tasks;
using AlinasRailTools.Shared.Heightmap;

namespace AlinasRailTools.Shared.DataSources;

/// <summary>
/// Synthetic heightmap source for testing and development.
/// Generates procedural terrain using simple noise functions.
/// </summary>
public class SyntheticHeightmapSource : IHeightmapSource
{
    private readonly SyntheticConfig _config;

    public string SourceName => "synthetic";

    public SyntheticHeightmapSource(SyntheticConfig config = null)
    {
        _config = config ?? new SyntheticConfig();
    }

    public Task<double[,]> GenerateHeightmapAsync(
        GameTileCoord gameTile,
        int resolution,
        double centerLat,
        double centerLng,
        double metersPerTile)
    {
        var heightmap = new double[resolution, resolution];

        // Calculate tile's real-world bounds
        double tileOffsetX = gameTile.X * metersPerTile;
        double tileOffsetY = gameTile.Y * metersPerTile;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Convert pixel to meters from world origin
                double pixelX = tileOffsetX + (x / (double)(resolution - 1)) * metersPerTile;
                double pixelY = tileOffsetY + (y / (double)(resolution - 1)) * metersPerTile;

                // Generate height using configured pattern
                heightmap[y, x] = GenerateHeight(pixelX, pixelY);
            }
        }

        return Task.FromResult(heightmap);
    }

    private double GenerateHeight(double x, double y)
    {
        return _config.Pattern switch
        {
            TerrainPattern.Flat => _config.BaseHeight,
            TerrainPattern.Slope => _config.BaseHeight + (x * _config.SlopeGradient),
            TerrainPattern.Sine => _config.BaseHeight +
                Math.Sin(x / _config.WaveLength) * _config.Amplitude +
                Math.Sin(y / _config.WaveLength) * _config.Amplitude,
            TerrainPattern.Perlin => _config.BaseHeight + PerlinNoise(x, y) * _config.Amplitude,
            _ => _config.BaseHeight
        };
    }

    /// <summary>
    /// Simple Perlin-like noise implementation for testing.
    /// Not cryptographically secure, just for procedural terrain.
    /// </summary>
    private double PerlinNoise(double x, double y)
    {
        // Scale coordinates
        x /= _config.NoiseScale;
        y /= _config.NoiseScale;

        // Get integer and fractional parts
        int x0 = (int)Math.Floor(x);
        int y0 = (int)Math.Floor(y);
        double dx = x - x0;
        double dy = y - y0;

        // Smooth the fractional parts
        double sx = Smoothstep(dx);
        double sy = Smoothstep(dy);

        // Generate corner gradients (pseudo-random based on coordinates)
        double n00 = DotGridGradient(x0, y0, x, y);
        double n10 = DotGridGradient(x0 + 1, y0, x, y);
        double n01 = DotGridGradient(x0, y0 + 1, x, y);
        double n11 = DotGridGradient(x0 + 1, y0 + 1, x, y);

        // Interpolate
        double nx0 = Lerp(n00, n10, sx);
        double nx1 = Lerp(n01, n11, sx);
        return Lerp(nx0, nx1, sy);
    }

    private static double Smoothstep(double t)
    {
        return t * t * (3.0 - 2.0 * t);
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + t * (b - a);
    }

    private double DotGridGradient(int ix, int iy, double x, double y)
    {
        // Pseudo-random gradient
        double angle = Hash(ix, iy) * 2.0 * Math.PI;
        double gx = Math.Cos(angle);
        double gy = Math.Sin(angle);

        // Distance vector
        double dx = x - ix;
        double dy = y - iy;

        return dx * gx + dy * gy;
    }

    private static double Hash(int x, int y)
    {
        // Simple hash function for pseudo-random values
        int h = x * 374761393 + y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        return (h & 0x7FFFFFFF) / (double)0x7FFFFFFF;
    }

    public object GetMetadata()
    {
        return new
        {
            source = SourceName,
            pattern = _config.Pattern.ToString().ToLower(),
            baseHeight = _config.BaseHeight,
            amplitude = _config.Amplitude,
            waveLength = _config.WaveLength,
            noiseScale = _config.NoiseScale,
            slopeGradient = _config.SlopeGradient
        };
    }
}

/// <summary>
/// Configuration for synthetic terrain generation.
/// </summary>
public class SyntheticConfig
{
    public TerrainPattern Pattern { get; set; } = TerrainPattern.Perlin;
    public double BaseHeight { get; set; } = 500.0;
    public double Amplitude { get; set; } = 100.0;
    public double WaveLength { get; set; } = 1000.0;
    public double NoiseScale { get; set; } = 500.0;
    public double SlopeGradient { get; set; } = 0.01;
}

public enum TerrainPattern
{
    Flat,
    Slope,
    Sine,
    Perlin
}
