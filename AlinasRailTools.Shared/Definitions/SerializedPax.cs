using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedPax
{
  [JsonProperty("spanIds")]
  public string[] SpanIds { get; set; } = [];

  [JsonProperty("industry")]
  public string Industry { get; set; } = "";

  [JsonProperty("timetableCode")]
  public string TimetableCode { get; set; } = "";

  [JsonProperty("basePopulation")]
  public int BasePopulation { get; set; } = 40;

  [JsonProperty("neighborIds")]
  public string[] NeighborIds { get; set; } = [];
}
