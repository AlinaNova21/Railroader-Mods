using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlinasRailTools.TileGenerator;

/// <summary>
/// Map configuration matching the Rust tool's Map.json format.
/// </summary>
public class MapConfig
{
    [JsonProperty("origin")]
    public Origin Origin { get; set; } = new Origin();

    [JsonProperty("tileDimension")]
    public double TileDimension { get; set; } = 500.0;

    [JsonProperty("tiles")]
    public List<TileCoord> Tiles { get; set; } = new List<TileCoord>();
}

public class Origin
{
    [JsonProperty("latitude")]
    public double Latitude { get; set; }

    [JsonProperty("longitude")]
    public double Longitude { get; set; }
}

public class TileCoord
{
    [JsonProperty("x")]
    public int X { get; set; }

    [JsonProperty("y")]
    public int Y { get; set; }
}

/// <summary>
/// Tool configuration (mapbox_token, etc.)
/// </summary>
public class ToolConfig
{
    [JsonProperty("mapbox_token")]
    public string? MapBoxToken { get; set; }

    [JsonProperty("game_directory")]
    public string? GameDirectory { get; set; }

    [JsonProperty("include_base_game")]
    public bool IncludeBaseGame { get; set; } = true;

    [JsonProperty("include_mods")]
    public bool IncludeMods { get; set; } = true;
}
