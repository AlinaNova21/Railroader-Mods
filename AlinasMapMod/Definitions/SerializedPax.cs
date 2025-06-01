using AlinasMapMod.Definitions.Converters;
using Newtonsoft.Json;

namespace AlinasMapMod.Definitions;
public class SerializedPax
{
  [JsonConverter(typeof(InlineJsonConverter))]
  public string[] SpanIds { get; set; }
  public string Industry { get; set; } = "";
  public string TimetableCode { get; set; } = "";
  public int BasePopulation { get; set; } = 40;
  [JsonConverter(typeof(InlineJsonConverter))]
  public string[] NeighborIds { get; set; } = [];
}
