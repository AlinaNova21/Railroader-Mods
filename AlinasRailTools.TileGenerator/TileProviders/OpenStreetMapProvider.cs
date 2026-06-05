namespace AlinasRailTools.TileGenerator.TileProviders;

/// <summary>
/// OpenStreetMap tile provider (free, no API key required).
/// </summary>
public class OpenStreetMapProvider : BaseTileProvider
{
    public override string Name => "OpenStreetMap";
    public override string Id => "openstreetmap";
    public override string CacheDirectory => "cache-osm";
    public override bool RequiresApiKey => false;

    public OpenStreetMapProvider(string cacheBaseDir) : base(cacheBaseDir)
    {
    }

    protected override string GetTileUrl(byte z, uint x, uint y)
    {
        // Use one of the OSM tile servers (rotating through a, b, c for load balancing)
        char server = (char)('a' + ((x + y) % 3));
        return $"https://{server}.tile.openstreetmap.org/{z}/{x}/{y}.png";
    }
}

/// <summary>
/// OpenTopoMap provider (topographic maps, free, no API key required).
/// </summary>
public class OpenTopoMapProvider : BaseTileProvider
{
    public override string Name => "OpenTopoMap";
    public override string Id => "opentopomap";
    public override string CacheDirectory => "cache-otm";
    public override bool RequiresApiKey => false;

    public OpenTopoMapProvider(string cacheBaseDir) : base(cacheBaseDir)
    {
    }

    protected override string GetTileUrl(byte z, uint x, uint y)
    {
        char server = (char)('a' + ((x + y) % 3));
        return $"https://{server}.tile.opentopomap.org/{z}/{x}/{y}.png";
    }
}
