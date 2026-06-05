using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedIndustry
{
  [JsonProperty("name")]
  public string Name { get; set; } = "";

  [JsonProperty("localPosition")]
  public SerializedVector3 LocalPosition { get; set; } = new SerializedVector3();

  [JsonProperty("usesContract")]
  public bool UsesContract { get; set; } = false;

  [JsonProperty("components")]
  public Dictionary<string, SerializedIndustryComponent> Components { get; set; } = new Dictionary<string, SerializedIndustryComponent>();
}
