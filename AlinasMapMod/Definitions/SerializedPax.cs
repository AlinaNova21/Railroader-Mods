using Track;

namespace AlinasMapMod.Definitions;
public class SerializedPax
{
  public string[] SpanIds { get; set; }
  public string Industry { get; set; } = "";
  public string TimetableCode { get; set; } = "";
  public int BasePopulation { get; set; } = 40;
  public string[] NeighborIds { get; set; } = [];
}
