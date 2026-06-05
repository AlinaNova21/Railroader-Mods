using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public enum TrackSpanPartEnd
{
  Start,
  End
}

public class TrackSpanPart
{
  [JsonProperty("segmentId")]
  public string SegmentId { get; set; } = "";

  [JsonProperty("distance")]
  public float Distance { get; set; } = 0;

  [JsonProperty("end")]
  public TrackSpanPartEnd End { get; set; } = TrackSpanPartEnd.Start;
}

public class SerializedTrackSpan
{
  [JsonProperty("upper")]
  public TrackSpanPart Upper { get; set; }

  [JsonProperty("lower")]
  public TrackSpanPart Lower { get; set; }
}
