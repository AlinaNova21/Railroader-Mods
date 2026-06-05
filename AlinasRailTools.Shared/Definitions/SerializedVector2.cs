using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public partial class SerializedVector2
{
  [JsonProperty("x")]
  public float X { get; set; }

  [JsonProperty("y")]
  public float Y { get; set; }
}
