using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedDelivery
{
  [JsonProperty("direction")]
  public int Direction { get; set; } = 0;

  [JsonProperty("count")]
  public int Count { get; set; } = 0;

  [JsonProperty("load")]
  public string Load { get; set; } = "";

  [JsonProperty("carTypeFilter")]
  public string CarTypeFilter { get; set; } = "";
}
