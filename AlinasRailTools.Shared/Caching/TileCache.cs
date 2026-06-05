#if NET8_0_OR_GREATER
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlinasRailTools.Shared.Heightmap;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AlinasRailTools.Shared.Caching;

/// <summary>
/// Two-tier caching system for MapBox tiles:
/// - L1: In-memory LRU cache for recently used tiles
/// - L2: Disk-based persistent cache
///
/// Thread-safe and supports deduplication of concurrent downloads via static in-flight tracking.
/// </summary>
public class TileCache
{
    private readonly string _cacheDir;
    private readonly LRUCache<(uint x, uint y, byte z), Image<Rgb24>> _memoryCache;

    private static readonly ConcurrentDictionary<string, Task<Image<Rgb24>>> _inFlightTiles = new();
    private static readonly SemaphoreSlim _inFlightLock = new(1, 1);

    public TileCache(string cacheDir, int memoryCacheSize = 50)
    {
        _cacheDir = cacheDir;
        _memoryCache = new LRUCache<(uint x, uint y, byte z), Image<Rgb24>>(memoryCacheSize);

        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Get the file path for a cached tile.
    /// Format: Maps/cache/z{zoom}/{x}_{y}.png
    /// </summary>
    public string GetTilePath(uint x, uint y, byte zoom)
    {
        string zoomDir = Path.Combine(_cacheDir, $"z{zoom}");
        return Path.Combine(zoomDir, $"{x}_{y}.png");
    }

    /// <summary>
    /// Check if a tile exists in the disk cache.
    /// </summary>
    public bool HasTile(uint x, uint y, byte zoom)
    {
        return File.Exists(GetTilePath(x, y, zoom));
    }

    /// <summary>
    /// Get a tile from cache (checks memory first, then disk).
    /// Returns null if not cached.
    /// </summary>
    public Image<Rgb24>? GetTile(uint x, uint y, byte zoom)
    {
        var key = (x, y, zoom);

        if (_memoryCache.TryGet(key, out var cachedImage))
        {
            return cachedImage.Clone();
        }

        string path = GetTilePath(x, y, zoom);
        if (!File.Exists(path))
        {
            return null;
        }

        var image = Image.Load<Rgb24>(path);
        _memoryCache.Add(key, image.Clone());

        return image;
    }

    /// <summary>
    /// Save a tile to disk cache.
    /// </summary>
    public void SaveTile(uint x, uint y, byte zoom, Image<Rgb24> image)
    {
        string path = GetTilePath(x, y, zoom);

        string? directory = Path.GetDirectoryName(path);
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        image.SaveAsPng(path);
        _memoryCache.Add((x, y, zoom), image.Clone());
    }

    /// <summary>
    /// Get a tile from cache, or fetch it if not cached. If another request is already
    /// fetching this tile, waits for that fetch to complete instead of starting a duplicate.
    /// </summary>
    public async Task<Image<Rgb24>> GetOrFetchTileAsync(
        uint x, uint y, byte zoom,
        Func<Task<Image<Rgb24>>> fetchFunc)
    {
        var key = (x, y, zoom);

        if (_memoryCache.TryGet(key, out var memImage))
        {
            return memImage.Clone();
        }

        var diskImage = await TryLoadFromDiskAsync(x, y, zoom).ConfigureAwait(false);
        if (diskImage != null)
        {
            _memoryCache.Add(key, diskImage.Clone());
            return diskImage;
        }

        string inFlightKey = $"{zoom}/{x}_{y}";

        await _inFlightLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Check again after acquiring lock - another request may have fetched it
            if (_memoryCache.TryGet(key, out memImage))
            {
                return memImage.Clone();
            }

            diskImage = await TryLoadFromDiskAsync(x, y, zoom).ConfigureAwait(false);
            if (diskImage != null)
            {
                _memoryCache.Add(key, diskImage.Clone());
                return diskImage;
            }

            if (_inFlightTiles.TryGetValue(inFlightKey, out var existingTask))
            {
                return await existingTask.ConfigureAwait(false);
            }

            var downloadTask = fetchFunc();
            _inFlightTiles[inFlightKey] = downloadTask;

            try
            {
                var result = await downloadTask.ConfigureAwait(false);

                await SaveToDiskAsync(x, y, zoom, result).ConfigureAwait(false);
                _memoryCache.Add(key, result.Clone());

                return result;
            }
            finally
            {
                _inFlightTiles.TryRemove(inFlightKey, out _);
            }
        }
        finally
        {
            _inFlightLock.Release();
        }
    }

    private async Task<Image<Rgb24>?> TryLoadFromDiskAsync(uint x, uint y, byte zoom)
    {
        string path = GetTilePath(x, y, zoom);
        if (!File.Exists(path))
        {
            return null;
        }

        return await Task.Run(() => Image.Load<Rgb24>(path)).ConfigureAwait(false);
    }

    private async Task SaveToDiskAsync(uint x, uint y, byte zoom, Image<Rgb24> image)
    {
        string path = GetTilePath(x, y, zoom);
        string? directory = Path.GetDirectoryName(path);

        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        await Task.Run(() => image.SaveAsPng(path)).ConfigureAwait(false);
    }

    /// <summary>
    /// Get all cached zoom levels (sorted highest to lowest).
    /// </summary>
    public List<byte> GetCachedZooms()
    {
        if (!Directory.Exists(_cacheDir))
        {
            return new List<byte>();
        }

        var zooms = new List<byte>();

        foreach (var dir in Directory.GetDirectories(_cacheDir))
        {
            string name = Path.GetFileName(dir);
            if (name.StartsWith("z") && byte.TryParse(name.Substring(1), out byte zoom))
            {
                zooms.Add(zoom);
            }
        }

        zooms.Sort();
        zooms.Reverse(); // Highest zoom first
        return zooms;
    }

    /// <summary>
    /// Find the best (highest) zoom level that has ALL required tiles cached.
    /// Returns null if no zoom level has all tiles.
    /// </summary>
    public byte? FindBestZoom(IEnumerable<MapBoxTileCoord> requiredTiles)
    {
        var zooms = GetCachedZooms();

        // Group required tiles by zoom level
        var tilesByZoom = requiredTiles
            .GroupBy(t => t.Zoom)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => t.GetTileIndices()).ToHashSet()
            );

        // Check each zoom level from highest to lowest
        foreach (byte zoom in zooms)
        {
            if (!tilesByZoom.TryGetValue(zoom, out var required))
            {
                continue;
            }

            bool allCached = required.All(tile => HasTile(tile.x, tile.y, zoom));

            if (allCached)
            {
                return zoom;
            }
        }

        return null;
    }

    /// <summary>
    /// Clear the memory cache (disk cache remains).
    /// </summary>
    public void ClearMemoryCache()
    {
        _memoryCache.Clear();
    }
}
#endif
