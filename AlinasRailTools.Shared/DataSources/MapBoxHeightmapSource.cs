#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AlinasRailTools.Shared.Caching;
using AlinasRailTools.Shared.Heightmap;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Serilog;

namespace AlinasRailTools.Shared.DataSources;

/// <summary>
/// MapBox heightmap source that downloads terrain-RGB tiles from MapBox API.
/// Uses tile cache to avoid redundant downloads.
/// </summary>
public class MapBoxHeightmapSource : IHeightmapSource
{
    private readonly string _apiKey;
    private readonly TileCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly byte _preferredZoom;

    public string SourceName => "mapbox";

    public MapBoxHeightmapSource(string apiKey, string cacheDir, byte preferredZoom = 14, ILogger logger = null)
        : this(apiKey, new TileCache(cacheDir), preferredZoom, logger)
    {
    }

    public MapBoxHeightmapSource(string apiKey, TileCache cache, byte preferredZoom = 14, ILogger logger = null)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _cache = cache;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AlinasRailTools.TileGenerator/0.1");
        _preferredZoom = preferredZoom;
        _logger = logger ?? Log.Logger;
    }

    public async Task<double[,]> GenerateHeightmapAsync(
        GameTileCoord gameTile,
        int resolution,
        double centerLat,
        double centerLng,
        double metersPerTile)
    {
        // Calculate the geographic bounds of this game tile
        var bounds = CalculateTileBounds(gameTile, centerLat, centerLng, metersPerTile, resolution);

        // Determine which MapBox tiles we need
        var requiredTiles = GetRequiredMapBoxTiles(bounds, _preferredZoom);

        _logger.Information("Game tile ({x},{y}) requires {count} MapBox tiles at zoom {zoom}",
            gameTile.X, gameTile.Y, requiredTiles.Count, _preferredZoom);

        // Load tiles (from cache or download)
        var tileImages = await LoadMapBoxTilesAsync(requiredTiles);

        // Merge into single heightmap
        var merged = MergeTiles(tileImages, requiredTiles);

        // Resample to game tile resolution
        var gameHeights = ResampleToGameGrid(merged, bounds, resolution, _preferredZoom);

        return gameHeights;
    }

    private TileBounds CalculateTileBounds(
        GameTileCoord gameTile,
        double centerLat,
        double centerLng,
        double metersPerTile,
        int resolution)
    {
        // Match game's TilePositionToLatLng exactly:
        // min = Origin.AddingMeters(TileDimension * tilePosition.y, TileDimension * tilePosition.x);
        // max = Origin.AddingMeters(TileDimension * (tilePosition.y + 1), TileDimension * (tilePosition.x + 1));

        // Calculate min (SW corner) - start of tile
        double minOffsetY = gameTile.Y * metersPerTile;
        double minOffsetX = gameTile.X * metersPerTile;

        // For min corner, use origin latitude for lat calculation
        double minLat = centerLat + (minOffsetY / CoordinateConverter.METERS_PER_DEGREE_LAT);
        // Then use minLat for longitude calculation (matches game's AddingMeters)
        double minLng = centerLng + (minOffsetX / CoordinateConverter.MetersPerDegreeLon(minLat));

        // Calculate max (NE corner) - end of tile
        double maxOffsetY = (gameTile.Y + 1) * metersPerTile;
        double maxOffsetX = (gameTile.X + 1) * metersPerTile;

        // For max corner, use origin latitude for lat calculation
        double maxLat = centerLat + (maxOffsetY / CoordinateConverter.METERS_PER_DEGREE_LAT);
        // Then use maxLat for longitude calculation (matches game's AddingMeters)
        double maxLng = centerLng + (maxOffsetX / CoordinateConverter.MetersPerDegreeLon(maxLat));

        return new TileBounds
        {
            MinLat = minLat,
            MaxLat = maxLat,
            MinLng = minLng,
            MaxLng = maxLng
        };
    }

    private List<MapBoxTileCoord> GetRequiredMapBoxTiles(TileBounds bounds, byte zoom)
    {
        // Get MapBox tile coordinates for corners
        var topLeft = CoordinateConverter.LatLngToMapBoxTile(new LatLng(bounds.MaxLat, bounds.MinLng), zoom);
        var bottomRight = CoordinateConverter.LatLngToMapBoxTile(new LatLng(bounds.MinLat, bounds.MaxLng), zoom);

        var (minX, minY) = topLeft.GetTileIndices();
        var (maxX, maxY) = bottomRight.GetTileIndices();

        var tiles = new List<MapBoxTileCoord>();
        for (uint y = minY; y <= maxY; y++)
        {
            for (uint x = minX; x <= maxX; x++)
            {
                tiles.Add(new MapBoxTileCoord(x, y, zoom));
            }
        }

        return tiles;
    }

    private async Task<Dictionary<(uint x, uint y), Image<Rgb24>>> LoadMapBoxTilesAsync(
        List<MapBoxTileCoord> requiredTiles)
    {
        var images = new Dictionary<(uint x, uint y), Image<Rgb24>>();

        var downloadTasks = requiredTiles.Select(tile => _cache.GetOrFetchTileAsync(
            tile.GetTileIndices().x,
            tile.GetTileIndices().y,
            tile.Zoom,
            () => DownloadMapBoxTileAsync(tile.GetTileIndices().x, tile.GetTileIndices().y, tile.Zoom)
        )).ToList();

        var results = await Task.WhenAll(downloadTasks).ConfigureAwait(false);

        int index = 0;
        foreach (var tile in requiredTiles)
        {
            var (x, y) = tile.GetTileIndices();
            images[(x, y)] = results[index];
            index++;
        }

        return images;
    }

    private async Task<Image<Rgb24>> DownloadMapBoxTileAsync(uint x, uint y, byte zoom)
    {
        // MapBox terrain-RGB tiles URL format
        string url = $"https://api.mapbox.com/v4/mapbox.terrain-rgb/{zoom}/{x}/{y}.pngraw?access_token={_apiKey}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync();
        return await Image.LoadAsync<Rgb24>(stream);
    }

    private MergedTileArray MergeTiles(
        Dictionary<(uint x, uint y), Image<Rgb24>> tiles,
        List<MapBoxTileCoord> requiredTiles)
    {
        if (tiles.Count == 0)
        {
            throw new InvalidOperationException("No tiles to merge");
        }

        // Find bounds
        uint minX = uint.MaxValue, minY = uint.MaxValue;
        uint maxX = 0, maxY = 0;

        foreach (var coord in requiredTiles)
        {
            var (x, y) = coord.GetTileIndices();
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        uint tileCountX = maxX - minX + 1;
        uint tileCountY = maxY - minY + 1;

        return MergedTileArray.FromTiles(tiles, minX, minY, tileCountX, tileCountY);
    }

    private double[,] ResampleToGameGrid(MergedTileArray merged, TileBounds bounds, int resolution, byte zoom)
    {
        var result = new double[resolution, resolution];

        // Match game's FetchHeights implementation exactly (MapboxLoader.cs lines 77-103)
        // Convert corners to tile coordinates using game's LatLongToTile (includes +0.5 offset)
        var tileCoord0 = CoordinateConverter.LatLngToMapBoxTile(new LatLng(bounds.MaxLat, bounds.MinLng), zoom);
        var tileCoord1 = CoordinateConverter.LatLngToMapBoxTile(new LatLng(bounds.MinLat, bounds.MaxLng), zoom);

        // Get integer tile indices
        int tileX0 = (int)tileCoord0.X;
        int tileY0 = (int)tileCoord0.Y;

        // Calculate dimensions - merged heightmap size and pixels per tile
        int tileSize = merged.TileSize;
        int mergedWidth = merged.Width;
        int mergedHeight = merged.Height;

        // Pixels per tile in each dimension
        int pixelsPerTileX = mergedWidth / (int)merged.TileCountX;
        int pixelsPerTileY = mergedHeight / (int)merged.TileCountY;

        // Calculate pixel sampling range (game's lines 89-92)
        // Uses fractional tile coordinates directly (includes the +0.5 offset from LatLongToTile)
        double startX = (tileCoord0.X - tileX0) * pixelsPerTileX;
        double startY = (tileCoord0.Y - tileY0) * pixelsPerTileY;
        double rangeX = (tileCoord1.X - tileCoord0.X) * pixelsPerTileX;
        double rangeY = (tileCoord1.Y - tileCoord0.Y) * pixelsPerTileY;

        // Match game's exact loop structure (MapboxLoader.cs lines 94-104)
        for (int i = 0; i < resolution; i++)
        {
            double x = startX + rangeX * i / (resolution - 1.0);
            for (int j = 0; j < resolution; j++)
            {
                double y = startY + rangeY * j / (resolution - 1.0);
                double value = merged.Sample(x, y);

                // Store directly without flip - the game's flip happens elsewhere
                result[j, i] = value;
            }
        }

        return result;
    }

    public object GetMetadata()
    {
        return new
        {
            source = SourceName,
            zoom = _preferredZoom,
            provider = "MapBox Terrain-RGB",
            apiVersion = "v4"
        };
    }

    private class TileBounds
    {
        public double MinLat { get; set; }
        public double MaxLat { get; set; }
        public double MinLng { get; set; }
        public double MaxLng { get; set; }
    }
}
#endif
