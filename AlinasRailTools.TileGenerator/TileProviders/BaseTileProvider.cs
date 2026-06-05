using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace AlinasRailTools.TileGenerator.TileProviders;

/// <summary>
/// Base implementation of ITileProvider with common caching logic.
/// </summary>
public abstract class BaseTileProvider : ITileProvider
{
    protected readonly string _cacheBaseDir;
    protected readonly HttpClient _httpClient;

    public abstract string Name { get; }
    public abstract string Id { get; }
    public abstract string CacheDirectory { get; }
    public abstract bool RequiresApiKey { get; }

    protected BaseTileProvider(string cacheBaseDir)
    {
        _cacheBaseDir = cacheBaseDir;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AlinasRailTools.TileGenerator/0.1");
    }

    /// <summary>
    /// Get the URL for downloading a tile.
    /// </summary>
    protected abstract string GetTileUrl(byte z, uint x, uint y);

    /// <summary>
    /// Optional: Transform tile data after download (e.g., for terrain-rgb visualization).
    /// Default implementation returns data unchanged.
    /// </summary>
    protected virtual async Task<byte[]> TransformTileData(byte[] data, byte z, uint x, uint y)
    {
        return await Task.FromResult(data);
    }

    public async Task<byte[]> GetTileAsync(byte z, uint x, uint y)
    {
        // Build cache path
        string cacheDir = Path.Combine(_cacheBaseDir, CacheDirectory);
        string cachePath = Path.Combine(cacheDir, z.ToString(), x.ToString(), $"{y}.png");

        // Check if cached
        if (File.Exists(cachePath))
        {
            return await File.ReadAllBytesAsync(cachePath);
        }

        try
        {
            // Download from provider
            string url = GetTileUrl(z, x, y);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var tileData = await response.Content.ReadAsByteArrayAsync();

            // Transform if needed
            tileData = await TransformTileData(tileData, z, x, y);

            // Save to cache
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            await File.WriteAllBytesAsync(cachePath, tileData);

            return tileData;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch tile {z}/{x}/{y} from provider {provider}", z, x, y, Name);
            throw;
        }
    }
}
