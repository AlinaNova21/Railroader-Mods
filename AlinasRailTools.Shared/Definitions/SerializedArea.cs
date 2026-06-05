using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedArea
{
  [JsonProperty("identifier")]
  public string Identifier { get; set; } = "";

  [JsonProperty("groupIds")]
  public List<string> GroupIds { get; set; } = new List<string>();
}
