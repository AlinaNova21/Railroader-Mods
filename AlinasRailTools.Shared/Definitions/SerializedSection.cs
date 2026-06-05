using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedSection
{
  [JsonProperty("displayName")]
  public string DisplayName { get; set; } = "";

  [JsonProperty("description")]
  public string Description { get; set; } = "";

  [JsonProperty("prerequisiteSections")]
  public Dictionary<string, bool> PrerequisiteSections { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("deliveryPhases")]
  public List<SerializedDeliveryPhase> DeliveryPhases { get; set; } = new List<SerializedDeliveryPhase>();

  [JsonProperty("disableFeaturesOnUnlock")]
  public Dictionary<string, bool> DisableFeaturesOnUnlock { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("enableFeaturesOnUnlock")]
  public Dictionary<string, bool> EnableFeaturesOnUnlock { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("enableFeaturesOnAvailable")]
  public Dictionary<string, bool> EnableFeaturesOnAvailable { get; set; } = new Dictionary<string, bool>();
}
