using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedLoader
{
  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; }

  [JsonProperty("rotation")]
  public SerializedVector3 Rotation { get; set; }

  [JsonProperty("prefab")]
  public string Prefab { get; set; } = "empty://";

  [JsonProperty("industry")]
  public string Industry { get; set; } = "";
}
