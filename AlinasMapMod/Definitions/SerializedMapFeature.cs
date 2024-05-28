using System.Collections.Generic;
using System.Linq;
using Game.Progression;

namespace AlinasMapMod.Definitions
{
  public class SerializedMapFeature
  {
    public string Description { get; set; } = "";
    public Dictionary<string, bool> Prerequisites { get; set; } = new Dictionary<string, bool>();
    public Dictionary<string, bool> AreasEnableOnUnlock { get; set; } = new Dictionary<string, bool>();
    public bool DefaultEnableInSandbox { get; set; } = false;
    public string DisplayName { get; set; } = "";
    public Dictionary<string, bool> GameObjectsEnableOnUnlock { get; set; } = new Dictionary<string, bool>();
    public Dictionary<string, bool> TrackGroupsAvailableOnUnlock { get; set; } = new Dictionary<string, bool>();
    public Dictionary<string, bool> TrackGroupsEnableOnUnlock { get; set; } = new Dictionary<string, bool>();
    public Dictionary<string, bool> UnlockExcludeIndustries { get; set; } = new Dictionary<string, bool>();
    public Dictionary<string, bool> UnlockIncludeIndustries { get; set; } = new Dictionary<string, bool>();
    public Dictionary<string, bool> UnlockIncludeIndustryComponents { get; set; } = new Dictionary<string, bool>();

    public SerializedMapFeature()
    {
    }
    public SerializedMapFeature(MapFeature feat)
    {
      Description = feat.description; // Assign a value to Description
      if (feat.prerequisites != null)
      {
        Prerequisites = feat.prerequisites.ToDictionary(p => p.identifier, p => true);
      }
      if (feat.areasEnableOnUnlock != null)
      {
        AreasEnableOnUnlock = feat.areasEnableOnUnlock.ToDictionary(a => a.identifier, a => true);
      }
      DefaultEnableInSandbox = feat.defaultEnableInSandbox;
      DisplayName = feat.displayName;
      if (feat.gameObjectsEnableOnUnlock != null)
      {
        GameObjectsEnableOnUnlock = feat.gameObjectsEnableOnUnlock.ToDictionary(g => Utils.getPathFromGameObject(g), g => true);
      }
      if (feat.trackGroupsAvailableOnUnlock != null)
      {
        TrackGroupsAvailableOnUnlock = feat.trackGroupsAvailableOnUnlock.ToDictionary(t => t, t => true);
      }
      if (feat.trackGroupsEnableOnUnlock != null)
      {
        TrackGroupsEnableOnUnlock = feat.trackGroupsEnableOnUnlock.ToDictionary(t => t, t => true);
      }
      if (feat.unlockExcludeIndustries != null)
      {
        UnlockExcludeIndustries = feat.unlockExcludeIndustries.ToDictionary(i => i.identifier, i => true);
      }
      if (feat.unlockIncludeIndustries != null)
      {
        UnlockIncludeIndustries = feat.unlockIncludeIndustries.ToDictionary(i => i.identifier, i => true);
      }
      if (feat.unlockIncludeIndustryComponents != null)
      {
        UnlockIncludeIndustryComponents = feat.unlockIncludeIndustryComponents.ToDictionary(i => i.Identifier, i => true);
      }
    }

    internal void ApplyTo(MapFeature feat, ObjectCache cache)
    {
      feat.description = Description;
      feat.defaultEnableInSandbox = DefaultEnableInSandbox;
      feat.displayName = DisplayName;
      feat.gameObjectsEnableOnUnlock = GameObjectsEnableOnUnlock.Keys.Select(Utils.gameObjectFromUri).ToArray();
      feat.prerequisites = Utils.ApplyList(feat.prerequisites ?? [], Prerequisites, cache.MapFeatures);
      feat.areasEnableOnUnlock = Utils.ApplyList(feat.areasEnableOnUnlock ?? [], AreasEnableOnUnlock, cache.Areas);
      feat.trackGroupsAvailableOnUnlock = Utils.ApplyList(feat.trackGroupsAvailableOnUnlock ?? [], TrackGroupsAvailableOnUnlock);
      feat.trackGroupsEnableOnUnlock = Utils.ApplyList(feat.trackGroupsEnableOnUnlock ?? [], TrackGroupsEnableOnUnlock);
      feat.unlockExcludeIndustries = Utils.ApplyList(feat.unlockExcludeIndustries ?? [], UnlockExcludeIndustries, cache.Industries);
      feat.unlockIncludeIndustries = Utils.ApplyList(feat.unlockIncludeIndustries ?? [], UnlockIncludeIndustries, cache.Industries);
      feat.unlockIncludeIndustryComponents = Utils.ApplyList(feat.unlockIncludeIndustryComponents ?? [], UnlockIncludeIndustryComponents, cache.IndustryComponents);
    }
  }
}
