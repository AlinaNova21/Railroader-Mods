using System.Collections.Generic;
using System.Linq;
using Game.Progression;

namespace AlinasMapMod.Definitions;

public class SerializedMapFeature
{
  public string Description { get; set; } = "";
  public Dictionary<string, bool> Prerequisites { get; set; } = [];
  public Dictionary<string, bool> AreasEnableOnUnlock { get; set; } = [];
  public bool DefaultEnableInSandbox { get; set; } = false;
  public string DisplayName { get; set; } = "";
  public Dictionary<string, bool> GameObjectsEnableOnUnlock { get; set; } = [];
  public Dictionary<string, bool> TrackGroupsAvailableOnUnlock { get; set; } = [];
  public Dictionary<string, bool> TrackGroupsEnableOnUnlock { get; set; } = [];
  public Dictionary<string, bool> UnlockExcludeIndustries { get; set; } = [];
  public Dictionary<string, bool> UnlockIncludeIndustries { get; set; } = [];
  public Dictionary<string, bool> UnlockIncludeIndustryComponents { get; set; } = [];

  public SerializedMapFeature()
  {
  }
  public SerializedMapFeature(MapFeature feat)
  {
    Description = feat.description; // Assign a value to Description
    if (feat.prerequisites != null) {
      Prerequisites = feat.prerequisites.ToDictionary(p => p.identifier, p => true);
    }
    if (feat.areasEnableOnUnlock != null) {
      AreasEnableOnUnlock = feat.areasEnableOnUnlock.ToDictionary(a => a.identifier, a => true);
    }
    DefaultEnableInSandbox = feat.defaultEnableInSandbox;
    DisplayName = feat.displayName;
    if (feat.gameObjectsEnableOnUnlock != null) {
      GameObjectsEnableOnUnlock = feat.gameObjectsEnableOnUnlock.ToDictionary(g => Utils.GetPathFromGameObject(g), g => true);
    }
    if (feat.trackGroupsAvailableOnUnlock != null) {
      TrackGroupsAvailableOnUnlock = feat.trackGroupsAvailableOnUnlock.ToDictionary(t => t, t => true);
    }
    if (feat.trackGroupsEnableOnUnlock != null) {
      TrackGroupsEnableOnUnlock = feat.trackGroupsEnableOnUnlock.ToDictionary(t => t, t => true);
    }
    if (feat.unlockExcludeIndustries != null) {
      UnlockExcludeIndustries = feat.unlockExcludeIndustries.ToDictionary(i => i.identifier, i => true);
    }
    if (feat.unlockIncludeIndustries != null) {
      UnlockIncludeIndustries = feat.unlockIncludeIndustries.ToDictionary(i => i.identifier, i => true);
    }
    if (feat.unlockIncludeIndustryComponents != null) {
      UnlockIncludeIndustryComponents = feat.unlockIncludeIndustryComponents.ToDictionary(i => i.Identifier, i => true);
    }
  }

  internal void ApplyTo(MapFeature feat)
  {
    feat.description = Description;
    feat.defaultEnableInSandbox = DefaultEnableInSandbox;
    feat.displayName = DisplayName;
    feat.gameObjectsEnableOnUnlock = GameObjectsEnableOnUnlock.Keys.Select(Utils.GameObjectFromUri).Where(g => g != null).ToArray();
    feat.prerequisites = DefinitionUtils.ApplyList(feat.prerequisites ?? [], Prerequisites);
    feat.areasEnableOnUnlock = DefinitionUtils.ApplyList(feat.areasEnableOnUnlock ?? [], AreasEnableOnUnlock);
    feat.trackGroupsAvailableOnUnlock = DefinitionUtils.ApplyList(feat.trackGroupsAvailableOnUnlock ?? [], TrackGroupsAvailableOnUnlock);
    feat.trackGroupsEnableOnUnlock = DefinitionUtils.ApplyList(feat.trackGroupsEnableOnUnlock ?? [], TrackGroupsEnableOnUnlock);
    feat.unlockExcludeIndustries = DefinitionUtils.ApplyList(feat.unlockExcludeIndustries ?? [], UnlockExcludeIndustries);
    feat.unlockIncludeIndustries = DefinitionUtils.ApplyList(feat.unlockIncludeIndustries ?? [], UnlockIncludeIndustries);
    feat.unlockIncludeIndustryComponents = DefinitionUtils.ApplyList(feat.unlockIncludeIndustryComponents ?? [], UnlockIncludeIndustryComponents);
  }
}
