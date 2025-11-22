using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Validation;
using AlinasMapMod.Caches;
using Game.Progression;
using UnityEngine;

namespace AlinasMapMod.Definitions;

public class SerializedMapFeature : SerializedComponentBase<MapFeature>,
  ICreatableComponent<MapFeature>,
  IDestroyableComponent<MapFeature>
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
    Read(feat);
  }

  protected override void ConfigureValidation()
  {
    RuleFor(() => DisplayName)
      .Required();

    // Validate GameObject URIs using Custom validation
    RuleFor(() => GameObjectsEnableOnUnlock)
      .Custom((gameObjects, context) =>
      {
        var result = new ValidationResult { IsValid = true };
        
        if (gameObjects != null)
        {
          foreach (var kvp in gameObjects)
          {
            var gameObjectUri = kvp.Key;
            if (string.IsNullOrEmpty(gameObjectUri))
            {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                Field = $"{nameof(GameObjectsEnableOnUnlock)}[{kvp.Key}]",
                Message = "GameObject URI cannot be null or empty",
                Code = "REQUIRED",
                Value = gameObjectUri
              });
            }
            // Additional URI validation could be added here
          }
        }
        
        return result;
      });
  }

  public override void Write(MapFeature feat)
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

  public override void Read(MapFeature feat)
  {
    Description = feat.description;
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
    // Validate before applying
    Validate();
    Write(feat);
  }

  public override MapFeature Create(string id)
  {
    // Find the MapFeatureManager (based on OldPatcher pattern)
    var mapFeatureManager = Object.FindObjectOfType<MapFeatureManager>(false);
    if (mapFeatureManager == null)
    {
      throw new System.InvalidOperationException("MapFeatureManager not found");
    }

    // Create GameObject and MapFeature component (based on OldPatcher pattern)
    var go = new GameObject(id);
    go.transform.SetParent(mapFeatureManager.transform);
    var mapFeature = go.AddComponent<MapFeature>();
    mapFeature.identifier = id;
    
    // Register in cache
    MapFeatureCache.Instance[id] = mapFeature;
    
    // Apply configuration
    Write(mapFeature);
    
    return mapFeature;
  }

  public void Destroy(MapFeature mapFeature)
  {
    // Destroy the GameObject (like MapLabel pattern)
    GameObject.Destroy(mapFeature.gameObject);
    
    // Remove from cache
    MapFeatureCache.Instance.Remove(mapFeature.identifier);
  }
}
