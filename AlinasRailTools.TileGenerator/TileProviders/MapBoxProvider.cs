using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using AlinasRailTools.Shared.DataSources;
using AlinasRailTools.TileGenerator.Api;

namespace AlinasRailTools.TileGenerator.TileProviders;

/// <summary>
/// Base class for MapBox tile providers.
/// </summary>
public abstract class MapBoxProvider : BaseTileProvider
{
    protected readonly string _apiKey;
    protected readonly string _layerId;

    public override bool RequiresApiKey => true;

    protected MapBoxProvider(string cacheBaseDir, string apiKey, string layerId)
        : base(cacheBaseDir)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _layerId = layerId;
    }
}

/// <summary>
/// MapBox Terrain-RGB provider with visualization.
/// </summary>
public class MapBoxTerrainProvider : MapBoxProvider
{
    private readonly string _visualizationMode;

    public override string Name => _visualizationMode == "color" ? "MapBox Terrain (Color)" : "MapBox Terrain (Grayscale)";
    public override string Id => _visualizationMode == "color" ? "mapbox-terrain-color" : "mapbox-terrain-grayscale";
    public override string CacheDirectory => "cache"; // Reuse existing cache

    public MapBoxTerrainProvider(string cacheBaseDir, string apiKey, string visualizationMode = "grayscale")
        : base(cacheBaseDir, apiKey, "mapbox.terrain-rgb")
    {
        _visualizationMode = visualizationMode;
    }

    protected override string GetTileUrl(byte z, uint x, uint y)
    {
        return $"https://api.mapbox.com/v4/{_layerId}/{z}/{x}/{y}.pngraw?access_token={_apiKey}";
    }

    protected override async Task<byte[]> TransformTileData(byte[] data, byte z, uint x, uint y)
    {
        // Load terrain-RGB data
        using var stream = new MemoryStream(data);
        var terrainRgb = await Image.LoadAsync<Rgb24>(stream);

        // Convert to visualization
        Image<Rgba32> output;
        if (_visualizationMode == "color")
        {
            output = VisualizationService.TerrainRgbToColorMap(terrainRgb);
        }
        else
        {
            output = VisualizationService.TerrainRgbToGrayscale(terrainRgb);
        }

        // Encode back to PNG
        var memoryStream = new MemoryStream();
        await output.SaveAsync(memoryStream, new PngEncoder());
        return memoryStream.ToArray();
    }
}

/// <summary>
/// MapBox Streets provider.
/// </summary>
public class MapBoxStreetsProvider : MapBoxProvider
{
    public override string Name => "MapBox Streets";
    public override string Id => "mapbox-streets";
    public override string CacheDirectory => "cache-streets";

    public MapBoxStreetsProvider(string cacheBaseDir, string apiKey)
        : base(cacheBaseDir, apiKey, "mapbox/streets-v12")
    {
    }

    protected override string GetTileUrl(byte z, uint x, uint y)
    {
        return $"https://api.mapbox.com/styles/v1/{_layerId}/tiles/{z}/{x}/{y}?access_token={_apiKey}";
    }
}

/// <summary>
/// MapBox Satellite provider.
/// </summary>
public class MapBoxSatelliteProvider : MapBoxProvider
{
    public override string Name => "MapBox Satellite";
    public override string Id => "mapbox-satellite";
    public override string CacheDirectory => "cache-satellite";

    public MapBoxSatelliteProvider(string cacheBaseDir, string apiKey)
        : base(cacheBaseDir, apiKey, "mapbox/satellite-v9")
    {
    }

    protected override string GetTileUrl(byte z, uint x, uint y)
    {
        return $"https://api.mapbox.com/styles/v1/{_layerId}/tiles/{z}/{x}/{y}?access_token={_apiKey}";
    }
}

/// <summary>
/// MapBox Outdoors provider.
/// </summary>
public class MapBoxOutdoorsProvider : MapBoxProvider
{
    public override string Name => "MapBox Outdoors";
    public override string Id => "mapbox-outdoors";
    public override string CacheDirectory => "cache-outdoors";

    public MapBoxOutdoorsProvider(string cacheBaseDir, string apiKey)
        : base(cacheBaseDir, apiKey, "mapbox/outdoors-v12")
    {
    }

    protected override string GetTileUrl(byte z, uint x, uint y)
    {
        return $"https://api.mapbox.com/styles/v1/{_layerId}/tiles/{z}/{x}/{y}?access_token={_apiKey}";
    }
}
