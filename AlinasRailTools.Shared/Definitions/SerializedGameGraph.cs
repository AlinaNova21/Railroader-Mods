using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedGameGraph
{
  [JsonProperty("tracks")]
  public SerializedTrackData Tracks { get; set; } = new SerializedTrackData();

  [JsonProperty("scenery")]
  public Dictionary<string, SerializedScenery> Scenery { get; set; } = new Dictionary<string, SerializedScenery>();

  [JsonProperty("areas")]
  public Dictionary<string, SerializedArea> Areas { get; set; } = new Dictionary<string, SerializedArea>();

  [JsonProperty("loads")]
  public Dictionary<string, SerializedLoad> Loads { get; set; } = new Dictionary<string, SerializedLoad>();

  [JsonProperty("splineys")]
  public Dictionary<string, SerializedSpliney> Splineys { get; set; } = new Dictionary<string, SerializedSpliney>();

  [JsonProperty("industries")]
  public Dictionary<string, SerializedIndustry> Industries { get; set; } = new Dictionary<string, SerializedIndustry>();
}

public class SerializedTrackData
{
  [JsonProperty("nodes")]
  public Dictionary<string, SerializedTrackNode> Nodes { get; set; } = new Dictionary<string, SerializedTrackNode>();

  [JsonProperty("segments")]
  public Dictionary<string, SerializedTrackSegment> Segments { get; set; } = new Dictionary<string, SerializedTrackSegment>();

  [JsonProperty("spans")]
  public Dictionary<string, SerializedTrackSpan> Spans { get; set; } = new Dictionary<string, SerializedTrackSpan>();
}
