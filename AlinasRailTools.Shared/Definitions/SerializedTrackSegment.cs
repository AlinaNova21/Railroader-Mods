using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public enum TrackSegmentStyle
{
  Standard,
  Tunnel,
  Bridge,
  Yard
}

public class SerializedTrackSegment
{
  [JsonProperty("startId")]
  public string StartId { get; set; } = "";

  [JsonProperty("endId")]
  public string EndId { get; set; } = "";

  [JsonProperty("priority")]
  public int Priority { get; set; } = 0;

  [JsonProperty("groupId")]
  public string GroupId { get; set; } = "";

  [JsonProperty("style")]
  public TrackSegmentStyle Style { get; set; } = TrackSegmentStyle.Standard;
}
