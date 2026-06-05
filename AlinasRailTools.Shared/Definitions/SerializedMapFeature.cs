using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedMapFeature
{
  [JsonProperty("description")]
  public string Description { get; set; } = "";

  [JsonProperty("prerequisites")]
  public Dictionary<string, bool> Prerequisites { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("areasEnableOnUnlock")]
  public Dictionary<string, bool> AreasEnableOnUnlock { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("defaultEnableInSandbox")]
  public bool DefaultEnableInSandbox { get; set; } = false;

  [JsonProperty("displayName")]
  public string DisplayName { get; set; } = "";

  [JsonProperty("gameObjectsEnableOnUnlock")]
  public Dictionary<string, bool> GameObjectsEnableOnUnlock { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("trackGroupsAvailableOnUnlock")]
  public Dictionary<string, bool> TrackGroupsAvailableOnUnlock { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("trackGroupsEnableOnUnlock")]
  public Dictionary<string, bool> TrackGroupsEnableOnUnlock { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("unlockExcludeIndustries")]
  public Dictionary<string, bool> UnlockExcludeIndustries { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("unlockIncludeIndustries")]
  public Dictionary<string, bool> UnlockIncludeIndustries { get; set; } = new Dictionary<string, bool>();

  [JsonProperty("unlockIncludeIndustryComponents")]
  public Dictionary<string, bool> UnlockIncludeIndustryComponents { get; set; } = new Dictionary<string, bool>();
}
