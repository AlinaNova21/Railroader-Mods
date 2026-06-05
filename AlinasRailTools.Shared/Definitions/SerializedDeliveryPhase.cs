using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedDeliveryPhase
{
  [JsonProperty("deliveries")]
  public List<SerializedDelivery> Deliveries { get; set; } = new List<SerializedDelivery>();

  [JsonProperty("cost")]
  public int Cost { get; set; } = 0;

  [JsonProperty("industryComponent")]
  public string IndustryComponent { get; set; } = "";
}
