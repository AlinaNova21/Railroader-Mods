using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedTurntable
{
  [JsonProperty("identifier")]
  public string Identifier { get; set; } = "";

  [JsonProperty("radius")]
  public int Radius { get; set; } = 15;

  [JsonProperty("subdivisions")]
  public int Subdivisions { get; set; } = 32;

  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; }

  [JsonProperty("rotation")]
  public SerializedVector3 Rotation { get; set; }

  [JsonProperty("roundhouseStalls")]
  public int RoundhouseStalls { get; set; } = 0;

  [JsonProperty("roundhouseTrackLength")]
  public int RoundhouseTrackLength { get; set; } = 46;

  [JsonProperty("stallPrefab")]
  public string StallPrefab { get; set; } = "vanilla://roundhouseStall";

  [JsonProperty("startPrefab")]
  public string StartPrefab { get; set; } = "vanilla://roundhouseStart";

  [JsonProperty("endPrefab")]
  public string EndPrefab { get; set; } = "vanilla://roundhouseEnd";
}
