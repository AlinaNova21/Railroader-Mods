using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedProgression
{
  [JsonProperty("sections")]
  public Dictionary<string, SerializedSection> Sections { get; set; } = new Dictionary<string, SerializedSection>();
}
