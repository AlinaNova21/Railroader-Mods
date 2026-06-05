using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using AlinasRailTools.Shared.DataSources;
using AlinasRailTools.Shared.Heightmap;
using AlinasRailTools.TileGenerator.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Serilog;

namespace AlinasRailTools.TileGenerator;

class Program
{
    private const string DefaultMapsDir = "Maps";
    private const string DefaultConfigFile = "config.json";

    static async Task<int> Main(string[] args)
    {
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            return await BuildCommandLine().InvokeAsync(args);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static RootCommand BuildCommandLine()
    {
        var rootCommand = new RootCommand("AlinasRailTools Tile Generator - Generate heightmap tiles for Railroader");

        // Init command
        var initCommand = new Command("init", "Initialize or update a map configuration");

        var mapNameArg = new Argument<string>("map-name", "Name of the map");

        var latOption = new Option<double>("--lat", "Origin latitude") { IsRequired = true };
        var lngOption = new Option<double>("--lng", "Origin longitude") { IsRequired = true };
        var tileDimOption = new Option<double>("--tile-dimension", () => 500.0, "Tile dimension in meters");
        var mapsDirOption = new Option<string>("--maps-dir", () => DefaultMapsDir, "Maps directory");

        initCommand.AddArgument(mapNameArg);
        initCommand.AddOption(latOption);
        initCommand.AddOption(lngOption);
        initCommand.AddOption(tileDimOption);
        initCommand.AddOption(mapsDirOption);

        initCommand.SetHandler((mapName, lat, lng, tileDim, mapsDir) =>
        {
            InitMap(mapName, lat, lng, tileDim, mapsDir);
        }, mapNameArg, latOption, lngOption, tileDimOption, mapsDirOption);

        // Generate command
        var generateCommand = new Command("generate", "Generate heightmap tiles");

        var genMapNameArg = new Argument<string>("map-name", "Name of the map");
        var tileXOption = new Option<int>("--tile-x", "Game tile X coordinate") { IsRequired = true };
        var tileYOption = new Option<int>("--tile-y", "Game tile Y coordinate") { IsRequired = true };
        var outputOption = new Option<string?>("--output", "Output file path (default: Maps/{map}/tile_{x}_{y}.data)");
        var sourceOption = new Option<string>("--source", () => "mapbox", "Data source (mapbox or synthetic)");
        var configFileOption = new Option<string>("--config", () => DefaultConfigFile, "Config file path");
        var genMapsDirOption = new Option<string>("--maps-dir", () => DefaultMapsDir, "Maps directory");
        var zoomOption = new Option<byte>("--zoom", () => (byte)15, "MapBox zoom level");

        generateCommand.AddArgument(genMapNameArg);
        generateCommand.AddOption(tileXOption);
        generateCommand.AddOption(tileYOption);
        generateCommand.AddOption(outputOption);
        generateCommand.AddOption(sourceOption);
        generateCommand.AddOption(configFileOption);
        generateCommand.AddOption(genMapsDirOption);
        generateCommand.AddOption(zoomOption);

        generateCommand.SetHandler(async (context) =>
        {
            var mapName = context.ParseResult.GetValueForArgument(genMapNameArg);
            var tileX = context.ParseResult.GetValueForOption(tileXOption);
            var tileY = context.ParseResult.GetValueForOption(tileYOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var source = context.ParseResult.GetValueForOption(sourceOption);
            var configFile = context.ParseResult.GetValueForOption(configFileOption);
            var mapsDir = context.ParseResult.GetValueForOption(genMapsDirOption);
            var zoom = context.ParseResult.GetValueForOption(zoomOption);

            await GenerateTileAsync(mapName!, tileX, tileY, output, source!, configFile!, mapsDir!, zoom);
        });

        // Validate command
        var validateCommand = new Command("validate", "Validate a heightmap tile's round-trip precision");
        var inputOption = new Option<string>("--input", "Input PNG file to validate") { IsRequired = true };

        validateCommand.AddOption(inputOption);
        validateCommand.SetHandler((input) =>
        {
            ValidateTile(input);
        }, inputOption);

        // Serve command (web server)
        var serveCommand = new Command("serve", "Start web server for interactive tile generation");
        var portOption = new Option<int>("--port", () => 5000, "HTTP port to listen on");
        var serveMapsDir = new Option<string>("--maps-dir", () => DefaultMapsDir, "Maps directory");
        var serveConfigFile = new Option<string>("--config", () => DefaultConfigFile, "Config file path");

        serveCommand.AddOption(portOption);
        serveCommand.AddOption(serveMapsDir);
        serveCommand.AddOption(serveConfigFile);

        serveCommand.SetHandler(async (port, mapsDir, configFile) =>
        {
            await StartWebServer(port, mapsDir, configFile);
        }, portOption, serveMapsDir, serveConfigFile);

        rootCommand.AddCommand(initCommand);
        rootCommand.AddCommand(generateCommand);
        rootCommand.AddCommand(validateCommand);
        rootCommand.AddCommand(serveCommand);

        return rootCommand;
    }

    static void InitMap(string mapName, double lat, double lng, double tileDimension, string mapsDir)
    {
        Log.Information("Initializing map: {mapName}", mapName);

        string mapDir = Path.Combine(mapsDir, mapName);
        Directory.CreateDirectory(mapDir);

        string mapJsonPath = Path.Combine(mapDir, "Map.json");

        var mapConfig = new MapConfig
        {
            Origin = new Origin { Latitude = lat, Longitude = lng },
            TileDimension = tileDimension,
            Tiles = new System.Collections.Generic.List<TileCoord>()
        };

        string json = JsonConvert.SerializeObject(mapConfig, Formatting.Indented);
        File.WriteAllText(mapJsonPath, json);

        Log.Information("Map configuration saved to: {path}", mapJsonPath);
        Log.Information("Origin: ({lat}, {lng})", lat, lng);
        Log.Information("Tile dimension: {dim}m", tileDimension);
    }

    static async Task GenerateTileAsync(
        string mapName,
        int tileX,
        int tileY,
        string? output,
        string source,
        string configFile,
        string mapsDir,
        byte zoom)
    {
        // Load map config
        string mapDir = Path.Combine(mapsDir, mapName);
        string mapJsonPath = Path.Combine(mapDir, "Map.json");

        if (!File.Exists(mapJsonPath))
        {
            Log.Error("Map not found: {path}. Use 'init' command to create it.", mapJsonPath);
            Environment.Exit(1);
        }

        var mapConfig = JsonConvert.DeserializeObject<MapConfig>(File.ReadAllText(mapJsonPath));
        if (mapConfig == null)
        {
            Log.Error("Failed to parse map config: {path}", mapJsonPath);
            Environment.Exit(1);
            return;
        }

        // Determine output path
        if (string.IsNullOrEmpty(output))
        {
            output = Path.Combine(mapDir, $"tile_{tileX:D3}_{tileY:D3}.data");
        }

        Log.Information("Generating tile ({x}, {y}) for map '{name}' using {source} source",
            tileX, tileY, mapName, source);
        Log.Information("Map origin: ({lat}, {lng}), tile dimension: {dim}m",
            mapConfig.Origin.Latitude, mapConfig.Origin.Longitude, mapConfig.TileDimension);

        var gameTile = new GameTileCoord(tileX, tileY);

        // Get heightmap source
        IHeightmapSource heightmapSource;

        if (source.ToLower() == "mapbox")
        {
            // Load API key
            string? apiKey = GetMapBoxApiKey(configFile);
            if (string.IsNullOrEmpty(apiKey))
            {
                Log.Error("MapBox API key not found. Set it in config.json, or MAPBOX_ACCESS_TOKEN environment variable.");
                Environment.Exit(1);
                return;
            }

            string cacheDir = Path.Combine(mapsDir, "cache");
            heightmapSource = new MapBoxHeightmapSource(apiKey, cacheDir, zoom, Log.Logger);
        }
        else if (source.ToLower() == "synthetic")
        {
            heightmapSource = new SyntheticHeightmapSource();
        }
        else
        {
            Log.Error("Unknown source: {source}. Use 'mapbox' or 'synthetic'", source);
            Environment.Exit(1);
            return;
        }

        Log.Information("Generating heightmap...");

        var heights = await heightmapSource.GenerateHeightmapAsync(
            gameTile,
            Shared.Heightmap.TileGenerator.TILE_RESOLUTION,
            mapConfig.Origin.Latitude,
            mapConfig.Origin.Longitude,
            mapConfig.TileDimension
        );

        var (min, max) = Sampling.GetMinMax(heights);
        var avg = Sampling.CalculateAverage(heights);

        Log.Information("Heightmap stats: min={min:F2}m, max={max:F2}m, avg={avg:F2}m", min, max, avg);

        Log.Information("Encoding to PNG...");
        Shared.Heightmap.TileGenerator.SaveTile(heights, output);

        Log.Information("Tile saved to: {output}", output);

        // Validate round-trip
        Log.Information("Validating round-trip precision...");
        var validation = Shared.Heightmap.TileGenerator.ValidateRoundTrip(heights);
        Log.Information("Validation: {result}", validation);

        if (!validation.IsAcceptable())
        {
            Log.Warning("Round-trip error exceeds 2cm threshold!");
        }
        else
        {
            Log.Information("Round-trip validation PASSED");
        }

        // Update Map.json with tile if not already present
        var tileCoord = new TileCoord { X = tileX, Y = tileY };
        if (!mapConfig.Tiles.Exists(t => t.X == tileX && t.Y == tileY))
        {
            mapConfig.Tiles.Add(tileCoord);
            string json = JsonConvert.SerializeObject(mapConfig, Formatting.Indented);
            File.WriteAllText(mapJsonPath, json);
            Log.Information("Added tile to map config");
        }

        // Log metadata
        var metadata = heightmapSource.GetMetadata();
        Log.Information("Source metadata: {metadata}", metadata);
    }

    static string? GetMapBoxApiKey(string configFile)
    {
        // Priority: config file, then environment variable
        if (File.Exists(configFile))
        {
            try
            {
                var config = JsonConvert.DeserializeObject<ToolConfig>(File.ReadAllText(configFile));
                if (!string.IsNullOrEmpty(config?.MapBoxToken))
                {
                    return config.MapBoxToken;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to read config file {file}: {error}", configFile, ex.Message);
            }
        }

        return Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
    }

    static void ValidateTile(string input)
    {
        Log.Information("Loading tile from: {input}", input);

        var heights = Shared.Heightmap.TileGenerator.LoadTile(input);

        var (min, max) = Sampling.GetMinMax(heights);
        var avg = Sampling.CalculateAverage(heights);

        Log.Information("Heightmap stats: min={min:F2}m, max={max:F2}m, avg={avg:F2}m", min, max, avg);

        Log.Information("Validating round-trip precision...");
        var validation = Shared.Heightmap.TileGenerator.ValidateRoundTrip(heights);
        Log.Information("Validation: {result}", validation);

        if (!validation.IsAcceptable())
        {
            Log.Warning("Round-trip error exceeds 2cm threshold!");
            Environment.Exit(1);
        }
        else
        {
            Log.Information("Round-trip validation PASSED");
        }
    }

    static async Task StartWebServer(int port, string mapsDir, string configFile)
    {
        Log.Information("Starting web server on port {port}...", port);
        Log.Information("Maps directory: {mapsDir}", Path.GetFullPath(mapsDir));
        Log.Information("Config file: {configFile}", Path.GetFullPath(configFile));

        var builder = WebApplication.CreateBuilder();

        // Configure Serilog for ASP.NET Core
        builder.Host.UseSerilog();

        var app = builder.Build();

        // Serve static files from wwwroot (Web SDK handles this automatically)
        app.UseStaticFiles();

        // Map API endpoints
        try
        {
            Log.Information("Registering API endpoints...");
            TileGeneratorApi.MapEndpoints(app, mapsDir, configFile);
            Log.Information("API endpoints registered successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register API endpoints");
            throw;
        }

        // Fallback to index.html for SPA routing
        app.MapFallbackToFile("index.html");

        Log.Information("Web server ready at http://localhost:{port}", port);
        Log.Information("Open your browser to http://localhost:{port} to access the interface", port);

        await app.RunAsync($"http://localhost:{port}");
    }
}
