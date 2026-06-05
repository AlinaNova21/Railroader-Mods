using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

/// <summary>
/// Main mod definition containing all game content
/// </summary>
public class SerializedModDefinition
{
  [JsonProperty("items")]
  public Dictionary<string, SerializedModItem> Items { get; set; } = new Dictionary<string, SerializedModItem>();

  [JsonProperty("turntables")]
  public Dictionary<string, SerializedTurntable> Turntables { get; set; } = new Dictionary<string, SerializedTurntable>();

  [JsonProperty("loaders")]
  public Dictionary<string, SerializedLoader> Loaders { get; set; } = new Dictionary<string, SerializedLoader>();

  [JsonProperty("stationAgents")]
  public Dictionary<string, SerializedStationAgent> StationAgents { get; set; } = new Dictionary<string, SerializedStationAgent>();

  [JsonProperty("mapLabels")]
  public Dictionary<string, SerializedMapLabel> MapLabels { get; set; } = new Dictionary<string, SerializedMapLabel>();

  [JsonProperty("telegraphPoles")]
  public SerializedTelegraphPoles TelegraphPoles { get; set; }

  [JsonProperty("progressions")]
  public Dictionary<string, SerializedProgression> Progressions { get; set; } = new Dictionary<string, SerializedProgression>();

  [JsonProperty("mapFeatures")]
  public Dictionary<string, SerializedMapFeature> MapFeatures { get; set; } = new Dictionary<string, SerializedMapFeature>();
}
