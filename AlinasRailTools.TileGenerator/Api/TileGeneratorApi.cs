using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using AlinasRailTools.Shared.Caching;
using AlinasRailTools.Shared.DataSources;
using AlinasRailTools.Shared.Heightmap;
using AlinasRailTools.TileGenerator.TileProviders;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace AlinasRailTools.TileGenerator.Api;

public static class TileGeneratorApi
{
    private const string DefaultMapsDir = "Maps";
    private const string DefaultConfigFile = "config.json";

    private static TileCache? _sharedMapBoxCache;
    private static readonly object _cacheLock = new object();

    private static TileCache GetSharedMapBoxCache(string mapsDir)
    {
        if (_sharedMapBoxCache == null)
        {
            lock (_cacheLock)
            {
                _sharedMapBoxCache ??= new TileCache(Path.Combine(mapsDir, "cache"));
            }
        }
        return _sharedMapBoxCache;
    }

    public static void MapEndpoints(IEndpointRouteBuilder app, string mapsDir, string configFile)
    {
        // Initialize tile provider registry with auto-detection
        string? apiKey = GetMapBoxApiKey(configFile);
        var providerRegistry = TileProviderRegistry.CreateDefault(mapsDir, apiKey);

        var sharedCache = GetSharedMapBoxCache(mapsDir);

        Log.Information("Registered {count} tile providers", providerRegistry.GetProviders().Count());
        foreach (var provider in providerRegistry.GetProviders())
        {
            Log.Information("  - {name} ({id})", provider.Name, provider.Id);
        }

        // List available tile providers
        app.MapGet("/api/tile-providers", () =>
        {
            return Results.Ok(new { providers = providerRegistry.GetProviderMetadata() });
        });

        // Generic tile endpoint using provider registry
        app.MapGet("/api/tiles/{providerId}/{z}/{x}/{y}", async (string providerId, byte z, uint x, uint y) =>
        {
            var provider = providerRegistry.GetProvider(providerId);
            if (provider == null)
            {
                return Results.NotFound(new { error = $"Provider '{providerId}' not found" });
            }

            try
            {
                var tileData = await provider.GetTileAsync(z, x, y);
                return Results.File(tileData, "image/png");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to serve tile {z}/{x}/{y} from provider {provider}", z, x, y, providerId);
                return Results.Problem($"Failed to serve tile: {ex.Message}");
            }
        });

        // List all maps
        app.MapGet("/api/maps", () =>
        {
            if (!Directory.Exists(mapsDir))
            {
                return Results.Ok(new { maps = Array.Empty<object>() });
            }

            var maps = Directory.GetDirectories(mapsDir)
                .Where(d => !Path.GetFileName(d).Equals("cache", StringComparison.OrdinalIgnoreCase))
                .Select(d =>
                {
                    var mapName = Path.GetFileName(d);
                    var mapJsonPath = Path.Combine(d, "Map.json");

                    if (!File.Exists(mapJsonPath))
                    {
                        return null;
                    }

                    try
                    {
                        var config = JsonConvert.DeserializeObject<MapConfig>(File.ReadAllText(mapJsonPath));
                        return new
                        {
                            name = mapName,
                            origin = config?.Origin,
                            tileDimension = config?.TileDimension,
                            tileCount = config?.Tiles?.Count ?? 0
                        };
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(m => m != null)
                .ToList();

            return Results.Ok(new { maps });
        });

        // Get specific map
        app.MapGet("/api/maps/{mapName}", (string mapName) =>
        {
            var mapDir = Path.Combine(mapsDir, mapName);
            var mapJsonPath = Path.Combine(mapDir, "Map.json");

            if (!File.Exists(mapJsonPath))
            {
                return Results.NotFound(new { error = "Map not found" });
            }

            try
            {
                var config = JsonConvert.DeserializeObject<MapConfig>(File.ReadAllText(mapJsonPath));
                if (config == null)
                {
                    return Results.Problem("Failed to parse map config");
                }

                // Discover additional tiles from base game and mods
                var discoveredTiles = new HashSet<(int x, int y)>();

                // Add tiles from Map.json
                foreach (var tile in config.Tiles)
                {
                    discoveredTiles.Add((tile.X, tile.Y));
                }

                // Scan for tiles in base game and mods
                var toolConfig = GetToolConfig(configFile);
                if (toolConfig != null && !string.IsNullOrEmpty(toolConfig.GameDirectory))
                {
                    // Base game tiles
                    var baseGameMapDir = Path.Combine(toolConfig.GameDirectory,
                        "Railroader_Data", "StreamingAssets", "Maps", mapName);
                    if (Directory.Exists(baseGameMapDir))
                    {
                        foreach (var file in Directory.GetFiles(baseGameMapDir, "tile_*.data"))
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file);
                            if (TryParseTileCoords(fileName, out int x, out int y))
                            {
                                discoveredTiles.Add((x, y));
                            }
                        }
                    }

                    // Mod tiles
                    var modsDir = Path.Combine(toolConfig.GameDirectory, "Mods");
                    if (Directory.Exists(modsDir))
                    {
                        foreach (var modDir in Directory.GetDirectories(modsDir))
                        {
                            var modMapDir = Path.Combine(modDir, "Maps", mapName);
                            if (Directory.Exists(modMapDir))
                            {
                                foreach (var file in Directory.GetFiles(modMapDir, "tile_*.data"))
                                {
                                    var fileName = Path.GetFileNameWithoutExtension(file);
                                    if (TryParseTileCoords(fileName, out int x, out int y))
                                    {
                                        discoveredTiles.Add((x, y));
                                    }
                                }
                            }
                        }
                    }
                }

                // Custom maps tiles
                if (Directory.Exists(mapDir))
                {
                    foreach (var file in Directory.GetFiles(mapDir, "tile_*.data"))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (TryParseTileCoords(fileName, out int x, out int y))
                        {
                            discoveredTiles.Add((x, y));
                        }
                    }
                }

                // Update config with all discovered tiles
                config.Tiles = discoveredTiles
                    .Select(t => new TileCoord { X = t.x, Y = t.y })
                    .OrderBy(t => t.X)
                    .ThenBy(t => t.Y)
                    .ToList();

                return Results.Ok(config);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to read map config: {ex.Message}");
            }
        });

        // Create new map
        app.MapPost("/api/maps", async (HttpRequest request) =>
        {
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(body);

            string? mapName = data?.name;
            double? lat = data?.latitude;
            double? lng = data?.longitude;
            double tileDimension = data?.tileDimension ?? 500.0;

            if (string.IsNullOrEmpty(mapName) || !lat.HasValue || !lng.HasValue)
            {
                return Results.BadRequest(new { error = "Missing required fields: name, latitude, longitude" });
            }

            string mapDir = Path.Combine(mapsDir, mapName);
            string mapJsonPath = Path.Combine(mapDir, "Map.json");

            if (File.Exists(mapJsonPath))
            {
                return Results.Conflict(new { error = "Map already exists" });
            }

            Directory.CreateDirectory(mapDir);

            var mapConfig = new MapConfig
            {
                Origin = new Origin { Latitude = lat.Value, Longitude = lng.Value },
                TileDimension = tileDimension,
                Tiles = new System.Collections.Generic.List<TileCoord>()
            };

            string json = JsonConvert.SerializeObject(mapConfig, Formatting.Indented);
            File.WriteAllText(mapJsonPath, json);

            Log.Information("Created map: {mapName} at ({lat}, {lng})", mapName, lat, lng);

            return Results.Ok(mapConfig);
        });

        // Generate tile(s)
        app.MapPost("/api/maps/{mapName}/generate", async (string mapName, HttpRequest request) =>
        {
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(body);

            int? tileX = data?.x;
            int? tileY = data?.y;
            byte zoom = data?.zoom ?? (byte)14;

            if (!tileX.HasValue || !tileY.HasValue)
            {
                return Results.BadRequest(new { error = "Missing required fields: x, y" });
            }

            var mapDir = Path.Combine(mapsDir, mapName);
            var mapJsonPath = Path.Combine(mapDir, "Map.json");

            if (!File.Exists(mapJsonPath))
            {
                return Results.NotFound(new { error = "Map not found" });
            }

            var mapConfig = JsonConvert.DeserializeObject<MapConfig>(File.ReadAllText(mapJsonPath));
            if (mapConfig == null)
            {
                return Results.Problem("Failed to parse map config");
            }

            // Get MapBox API key
            string? apiKey = GetMapBoxApiKey(configFile);
            if (string.IsNullOrEmpty(apiKey))
            {
                return Results.Problem("MapBox API key not configured");
            }

            try
            {
                var output = Path.Combine(mapDir, $"tile_{tileX.Value:D3}_{tileY.Value:D3}.data");
                var gameTile = new GameTileCoord(tileX.Value, tileY.Value);

                var heightmapSource = new MapBoxHeightmapSource(apiKey, sharedCache, zoom, Log.Logger);

                Log.Information("Generating tile ({x}, {y}) for map '{name}'", tileX, tileY, mapName);

                var heights = await heightmapSource.GenerateHeightmapAsync(
                    gameTile,
                    Shared.Heightmap.TileGenerator.TILE_RESOLUTION,
                    mapConfig.Origin.Latitude,
                    mapConfig.Origin.Longitude,
                    mapConfig.TileDimension
                );

                var (min, max) = Sampling.GetMinMax(heights);
                var avg = Sampling.CalculateAverage(heights);

                Shared.Heightmap.TileGenerator.SaveTile(heights, output);

                // Update Map.json
                var tileCoord = new TileCoord { X = tileX.Value, Y = tileY.Value };
                if (!mapConfig.Tiles.Exists(t => t.X == tileX && t.Y == tileY))
                {
                    mapConfig.Tiles.Add(tileCoord);
                    string json = JsonConvert.SerializeObject(mapConfig, Formatting.Indented);
                    File.WriteAllText(mapJsonPath, json);
                }

                Log.Information("Tile generated: {output}", output);

                return Results.Ok(new
                {
                    success = true,
                    x = tileX,
                    y = tileY,
                    min = min,
                    max = max,
                    avg = avg,
                    path = output
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate tile ({x}, {y})", tileX, tileY);
                return Results.Problem($"Tile generation failed: {ex.Message}");
            }
        });

        // Serve MapBox tiles with visualization
        app.MapGet("/api/mapbox-tiles/{z}/{x}/{y}", async (byte z, uint x, uint y, string? mode) =>
        {
            string? apiKey = GetMapBoxApiKey(configFile);
            if (string.IsNullOrEmpty(apiKey))
            {
                return Results.Problem("MapBox API key not configured");
            }

            try
            {
                string cacheDir = Path.Combine(mapsDir, "cache");
                var cache = new TileCache(cacheDir);

                // Check cache
                var cachedTile = cache.GetTile(x, y, z);
                Image<Rgb24> terrainRgb;

                if (cachedTile != null)
                {
                    terrainRgb = cachedTile;
                }
                else
                {
                    // Download from MapBox
                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "AlinasRailTools.TileGenerator/0.1");
                    string url = $"https://api.mapbox.com/v4/mapbox.terrain-rgb/{z}/{x}/{y}.pngraw?access_token={apiKey}";
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var stream = await response.Content.ReadAsStreamAsync();
                    terrainRgb = await Image.LoadAsync<Rgb24>(stream);

                    // Save to cache
                    cache.SaveTile(x, y, z, terrainRgb);
                }

                // Convert to displayable image
                Image<Rgba32> output;
                if (mode == "color")
                {
                    output = VisualizationService.TerrainRgbToColorMap(terrainRgb);
                }
                else
                {
                    output = VisualizationService.TerrainRgbToGrayscale(terrainRgb);
                }

                var memoryStream = new MemoryStream();
                await output.SaveAsync(memoryStream, new PngEncoder());
                memoryStream.Position = 0;

                return Results.File(memoryStream, "image/png");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to serve MapBox tile {z}/{x}/{y}", z, x, y);
                return Results.Problem($"Failed to serve tile: {ex.Message}");
            }
        });

        // Serve MapBox street tiles
        app.MapGet("/api/mapbox-streets/{z}/{x}/{y}", async (byte z, uint x, uint y) =>
        {
            string? apiKey = GetMapBoxApiKey(configFile);
            if (string.IsNullOrEmpty(apiKey))
            {
                return Results.Problem("MapBox API key not configured");
            }

            try
            {
                string cacheDir = Path.Combine(mapsDir, "cache-streets");
                Directory.CreateDirectory(cacheDir);

                // Build cache path
                string cachePath = Path.Combine(cacheDir, z.ToString(), x.ToString(), $"{y}.png");

                // Check if cached
                if (File.Exists(cachePath))
                {
                    return Results.File(cachePath, "image/png");
                }

                // Download from MapBox
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "AlinasRailTools.TileGenerator/0.1");
                string url = $"https://api.mapbox.com/styles/v1/mapbox/streets-v12/tiles/{z}/{x}/{y}?access_token={apiKey}";
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var tileData = await response.Content.ReadAsByteArrayAsync();

                // Save to cache
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                await File.WriteAllBytesAsync(cachePath, tileData);

                return Results.File(tileData, "image/png");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to serve MapBox streets tile {z}/{x}/{y}", z, x, y);
                return Results.Problem($"Failed to serve tile: {ex.Message}");
            }
        });

        // Serve generated game tiles
        app.MapGet("/api/game-tiles/{mapName}/{x}/{y}", async (string mapName, int x, int y, bool? includeBaseGame, bool? includeMods) =>
        {
            try
            {
                var searchPaths = new List<string>();
                var tileBaseName = $"tile_{x:D3}_{y:D3}";

                // Always check custom maps dir first (only .data)
                searchPaths.Add(Path.Combine(mapsDir, mapName, tileBaseName + ".data"));

                // Load config to check game directory settings
                var config = GetToolConfig(configFile);
                if (config != null && !string.IsNullOrEmpty(config.GameDirectory))
                {
                    // Use query params if provided, otherwise fall back to config
                    bool shouldIncludeBaseGame = includeBaseGame ?? config.IncludeBaseGame;
                    bool shouldIncludeMods = includeMods ?? config.IncludeMods;

                    if (shouldIncludeBaseGame)
                    {
                        // Check base game tiles (.data)
                        searchPaths.Add(Path.Combine(config.GameDirectory,
                            "Railroader_Data", "StreamingAssets", "Maps", mapName, tileBaseName + ".data"));
                    }

                    if (shouldIncludeMods)
                    {
                        // Check all mods for this map (.data)
                        var modsDir = Path.Combine(config.GameDirectory, "Mods");
                        if (Directory.Exists(modsDir))
                        {
                            foreach (var modDir in Directory.GetDirectories(modsDir))
                            {
                                searchPaths.Add(Path.Combine(modDir, "Maps", mapName, tileBaseName + ".data"));
                            }
                        }
                    }
                }

                // Return first found tile
                foreach (var path in searchPaths)
                {
                    if (File.Exists(path))
                    {
                        Log.Information("Serving game tile from: {path}", path);

                        // Load the heightmap tile and convert to grayscale for visualization
                        var heightmap = Shared.Heightmap.TileGenerator.LoadTile(path);
                        var (min, max) = Sampling.GetMinMax(heightmap);

                        // Convert heightmap array to displayable image
                        var resolution = heightmap.GetLength(0); // Assuming square tiles
                        var image = new Image<Rgba32>(resolution, resolution);

                        image.ProcessPixelRows(accessor =>
                        {
                            for (int py = 0; py < resolution; py++)
                            {
                                var row = accessor.GetRowSpan(py);
                                for (int px = 0; px < resolution; px++)
                                {
                                    var height = heightmap[py, px];
                                    var normalized = (height - min) / (max - min);
                                    normalized = Math.Clamp(normalized, 0, 1);
                                    byte gray = (byte)(normalized * 255);
                                    row[px] = new Rgba32(gray, gray, gray, 255);
                                }
                            }
                        });

                        var memoryStream = new MemoryStream();
                        await image.SaveAsync(memoryStream, new PngEncoder());
                        memoryStream.Position = 0;

                        return Results.File(memoryStream, "image/png");
                    }
                }

                Log.Warning("Game tile not found: {mapName} ({x}, {y})", mapName, x, y);
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error serving game tile {mapName} ({x}, {y})", mapName, x, y);
                return Results.Problem($"Error serving tile: {ex.Message}");
            }
        });

        // Tile pyramid endpoint - dynamically composite game tiles into web map tiles
        app.MapGet("/api/game-tile-pyramid/{mapName}/{z}/{x}/{y}", async (string mapName, int z, int x, int y, bool? includeBaseGame, bool? includeMods) =>
        {
            try
            {
                var mapDir = Path.Combine(mapsDir, mapName);
                var mapJsonPath = Path.Combine(mapDir, "Map.json");

                if (!File.Exists(mapJsonPath))
                {
                    return Results.NotFound(new { error = "Map not found" });
                }

                var mapConfig = JsonConvert.DeserializeObject<MapConfig>(File.ReadAllText(mapJsonPath));
                if (mapConfig == null)
                {
                    return Results.Problem("Failed to parse map config");
                }

                var config = GetToolConfig(configFile);
                bool shouldIncludeBaseGame = includeBaseGame ?? config?.IncludeBaseGame ?? true;
                bool shouldIncludeMods = includeMods ?? config?.IncludeMods ?? true;

                // Create a 256x256 tile image
                var tileImage = new Image<Rgba32>(256, 256);

                // Calculate which game tiles are covered by this web tile
                // Web tile coordinates convert to lat/lng bounds
                double n = Math.PI - 2.0 * Math.PI * y / Math.Pow(2.0, z);
                double lat1 = 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
                double lon1 = x / Math.Pow(2.0, z) * 360.0 - 180.0;

                n = Math.PI - 2.0 * Math.PI * (y + 1) / Math.Pow(2.0, z);
                double lat2 = 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
                double lon2 = (x + 1) / Math.Pow(2.0, z) * 360.0 - 180.0;

                // Find game tiles that intersect this web tile
                var swGameTile = CoordinateConverter.LatLngToGameTile(
                    Math.Min(lat1, lat2), Math.Min(lon1, lon2),
                    mapConfig.Origin.Latitude, mapConfig.Origin.Longitude, mapConfig.TileDimension);
                var neGameTile = CoordinateConverter.LatLngToGameTile(
                    Math.Max(lat1, lat2), Math.Max(lon1, lon2),
                    mapConfig.Origin.Latitude, mapConfig.Origin.Longitude, mapConfig.TileDimension);

                // Use a fixed height range for consistent normalization across all tiles
                const double globalMinHeight = 0.0;
                const double globalMaxHeight = 3000.0;

                bool anyTileFound = false;

                // Build list of all tiles to load
                var tilesToLoad = new List<(int gx, int gy, string tilePath)>();

                for (int gy = swGameTile.Y; gy <= neGameTile.Y; gy++)
                {
                    for (int gx = swGameTile.X; gx <= neGameTile.X; gx++)
                    {
                        var tileBaseName = $"tile_{gx:D3}_{gy:D3}";
                        string? tilePath = null;

                        // Search for tile (custom → base game → mods)
                        var searchPaths = new List<string>();
                        searchPaths.Add(Path.Combine(mapsDir, mapName, tileBaseName + ".data"));

                        if (config != null && !string.IsNullOrEmpty(config.GameDirectory))
                        {
                            if (shouldIncludeBaseGame)
                            {
                                searchPaths.Add(Path.Combine(config.GameDirectory,
                                    "Railroader_Data", "StreamingAssets", "Maps", mapName, tileBaseName + ".data"));
                            }

                            if (shouldIncludeMods)
                            {
                                var modsDir = Path.Combine(config.GameDirectory, "Mods");
                                if (Directory.Exists(modsDir))
                                {
                                    foreach (var modDir in Directory.GetDirectories(modsDir))
                                    {
                                        searchPaths.Add(Path.Combine(modDir, "Maps", mapName, tileBaseName + ".data"));
                                    }
                                }
                            }
                        }

                        foreach (var path in searchPaths)
                        {
                            if (File.Exists(path))
                            {
                                tilePath = path;
                                break;
                            }
                        }

                        if (tilePath != null)
                        {
                            tilesToLoad.Add((gx, gy, tilePath));
                        }
                    }
                }

                // Load all tiles in parallel for maximum performance
                var loadedTiles = await Task.WhenAll(tilesToLoad.Select(async tile =>
                {
                    var heightmap = await Task.Run(() => Shared.Heightmap.TileGenerator.LoadTile(tile.tilePath));
                    return (tile.gx, tile.gy, heightmap);
                }));

                // Composite all loaded tiles
                foreach (var (gx, gy, heightmap) in loadedTiles)
                {
                    anyTileFound = true;
                        // Heightmap is 513x513, use full resolution including overlap for seamless tiling
                        const int usableResolution = 513;

                        // Calculate where this game tile appears in the web tile
                        var (minLat, minLng, maxLat, maxLng) = CoordinateConverter.GameTileToBounds(
                            gx, gy, mapConfig.Origin.Latitude, mapConfig.Origin.Longitude, mapConfig.TileDimension);

                        // Map game tile bounds to web tile pixel coordinates
                        // Use Math.Round for proper rounding instead of truncation to avoid gaps
                        int px1 = (int)Math.Round((minLng - lon1) / (lon2 - lon1) * 256);
                        int py1 = (int)Math.Round((lat1 - maxLat) / (lat1 - lat2) * 256);
                        int px2 = (int)Math.Round((maxLng - lon1) / (lon2 - lon1) * 256);
                        int py2 = (int)Math.Round((lat1 - minLat) / (lat1 - lat2) * 256);

                        px1 = Math.Clamp(px1, 0, 256);
                        py1 = Math.Clamp(py1, 0, 256);
                        px2 = Math.Clamp(px2, 0, 256);
                        py2 = Math.Clamp(py2, 0, 256);

                        if (px2 <= px1 || py2 <= py1) continue;

                        // Draw game tile into web tile
                        tileImage.ProcessPixelRows(accessor =>
                        {
                            for (int py = py1; py < py2; py++)
                            {
                                if (py < 0 || py >= 256) continue;
                                var row = accessor.GetRowSpan(py);
                                for (int px = px1; px < px2; px++)
                                {
                                    if (px < 0 || px >= 256) continue;

                                    // Sample from heightmap using full 513x513 resolution including overlap
                                    // Map pixel position within the game tile to heightmap coordinates
                                    double fracX = (px - px1) / (double)(px2 - px1);
                                    double fracY = (py - py1) / (double)(py2 - py1);
                                    int hx = (int)(fracX * (usableResolution - 1));
                                    int hy = (int)(fracY * (usableResolution - 1));
                                    hx = Math.Clamp(hx, 0, usableResolution - 1);
                                    hy = Math.Clamp(hy, 0, usableResolution - 1);

                                    var height = heightmap[hy, hx];
                                    var normalized = (height - globalMinHeight) / (globalMaxHeight - globalMinHeight);
                                    normalized = Math.Clamp(normalized, 0, 1);
                                    byte gray = (byte)(normalized * 255);
                                    row[px] = new Rgba32(gray, gray, gray, 255);
                                }
                            }
                        });
                }

                if (!anyTileFound)
                {
                    return Results.NotFound();
                }

                var memoryStream = new MemoryStream();
                await tileImage.SaveAsync(memoryStream, new PngEncoder());
                memoryStream.Position = 0;

                return Results.File(memoryStream, "image/png");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error serving tile pyramid {mapName}/{z}/{x}/{y}", mapName, z, x, y);
                return Results.Problem($"Error serving tile: {ex.Message}");
            }
        });

        // Cache statistics
        app.MapGet("/api/cache/stats", () =>
        {
            string cacheDir = Path.Combine(mapsDir, "cache");
            if (!Directory.Exists(cacheDir))
            {
                return Results.Ok(new { zoomLevels = Array.Empty<object>() });
            }

            var cache = new TileCache(cacheDir);
            var zooms = cache.GetCachedZooms();

            var stats = zooms.Select(z =>
            {
                var zoomDir = Path.Combine(cacheDir, $"z{z}");
                var tileCount = Directory.Exists(zoomDir)
                    ? Directory.GetFiles(zoomDir, "*.png").Length
                    : 0;

                return new { zoom = z, tileCount };
            }).ToList();

            return Results.Ok(new { zoomLevels = stats });
        });
    }

    private static ToolConfig? GetToolConfig(string configFile)
    {
        if (File.Exists(configFile))
        {
            try
            {
                return JsonConvert.DeserializeObject<ToolConfig>(File.ReadAllText(configFile));
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to read config file {file}: {error}", configFile, ex.Message);
            }
        }

        return null;
    }

    private static string? GetMapBoxApiKey(string configFile)
    {
        var config = GetToolConfig(configFile);
        if (!string.IsNullOrEmpty(config?.MapBoxToken))
        {
            return config.MapBoxToken;
        }

        return Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
    }

    private static bool TryParseTileCoords(string fileName, out int x, out int y)
    {
        // Expected format: tile_XXX_YYY where XXX and YYY are zero-padded integers (can be negative)
        x = 0;
        y = 0;

        if (!fileName.StartsWith("tile_"))
        {
            return false;
        }

        var parts = fileName.Substring(5).Split('_');
        if (parts.Length != 2)
        {
            return false;
        }

        return int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
    }

    private static string? FindTilePath(string mapsDir, string configFile, string mapName, int tileX, int tileY, bool includeBaseGame, bool includeMods)
    {
        var tileBaseName = $"tile_{tileX:D3}_{tileY:D3}";
        var searchPaths = new List<string>();

        // Always check custom maps dir first
        searchPaths.Add(Path.Combine(mapsDir, mapName, tileBaseName + ".data"));

        var config = GetToolConfig(configFile);
        if (config != null && !string.IsNullOrEmpty(config.GameDirectory))
        {
            if (includeBaseGame)
            {
                searchPaths.Add(Path.Combine(config.GameDirectory,
                    "Railroader_Data", "StreamingAssets", "Maps", mapName, tileBaseName + ".data"));
            }

            if (includeMods)
            {
                var modsDir = Path.Combine(config.GameDirectory, "Mods");
                if (Directory.Exists(modsDir))
                {
                    foreach (var modDir in Directory.GetDirectories(modsDir))
                    {
                        searchPaths.Add(Path.Combine(modDir, "Maps", mapName, tileBaseName + ".data"));
                    }
                }
            }
        }

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }
}
