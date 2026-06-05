using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedModItem
{
  [JsonProperty("identifier")]
  public string Identifier { get; set; } = "";

  [JsonProperty("name")]
  public string Name { get; set; } = "";

  [JsonProperty("groupIds")]
  public string[] GroupIds { get; set; } = [];

  [JsonProperty("description")]
  public string Description { get; set; } = "";

  [JsonProperty("prerequisiteSections")]
  public string[] PrerequisiteSections { get; set; } = [];

  [JsonProperty("deliveryPhases")]
  public SerializedDeliveryPhase[] DeliveryPhases { get; set; } = [];

  [JsonProperty("area")]
  public string Area { get; set; } = "";

  [JsonProperty("trackSpans")]
  public string[] TrackSpans { get; set; } = [];

  [JsonProperty("industryComponent")]
  public string IndustryComponent { get; set; } = "";
}
