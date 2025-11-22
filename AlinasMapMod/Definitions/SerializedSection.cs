using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Validation;
using AlinasMapMod.Caches;
using Game.Progression;
using UnityEngine;

namespace AlinasMapMod.Definitions;

public class SerializedSection : SerializedComponentBase<Section>,
  ICreatableComponent<Section>,
  IDestroyableComponent<Section>
{
  public string DisplayName { get; set; } = "";
  public string Description { get; set; } = "";
  public Dictionary<string, bool> PrerequisiteSections { get; set; } = new Dictionary<string, bool>();
  public IEnumerable<SerializedDeliveryPhase> DeliveryPhases { get; set; } = new List<SerializedDeliveryPhase>();
  public Dictionary<string, bool> DisableFeaturesOnUnlock { get; set; } = new Dictionary<string, bool>();
  public Dictionary<string, bool> EnableFeaturesOnUnlock { get; set; } = new Dictionary<string, bool>();
  public Dictionary<string, bool> EnableFeaturesOnAvailable { get; set; } = new Dictionary<string, bool>();

  public SerializedSection()
  {
  }

  public SerializedSection(Section s)
  {
    Read(s);
  }

  protected override void ConfigureValidation()
  {
    RuleFor(() => DisplayName)
      .Required();
    
    RuleFor(() => Description)
      .Required();

    // Validate delivery phases - will be validated when Write is called
  }

  public override Section Create(string id)
  {
    // Create GameObject with Section component (based on progression patterns)
    var go = new GameObject(id);
    var section = go.AddComponent<Section>();
    section.identifier = id;
    
    // Register in cache
    SectionCache.Instance[id] = section;
    
    // Apply configuration
    Write(section);
    
    return section;
  }

  public override void Write(Section section)
  {
    section.displayName = DisplayName;
    section.description = Description;
    section.prerequisiteSections = DefinitionUtils.ApplyList(section.prerequisiteSections ?? [], PrerequisiteSections);
    section.disableFeaturesOnUnlock = DefinitionUtils.ApplyList(section.disableFeaturesOnUnlock ?? [], DisableFeaturesOnUnlock);
    section.enableFeaturesOnUnlock = DefinitionUtils.ApplyList(section.enableFeaturesOnUnlock ?? [], EnableFeaturesOnUnlock);
    section.enableFeaturesOnAvailable = DefinitionUtils.ApplyList(section.enableFeaturesOnAvailable ?? [], EnableFeaturesOnAvailable);

    section.deliveryPhases = DeliveryPhases.Select(dp => {
      var phase = new Section.DeliveryPhase();
      dp.Write(phase); // Use new Write method
      return phase;
    }).ToArray();
  }

  public override void Read(Section section)
  {
    DisplayName = section.displayName;
    Description = section.description;
    PrerequisiteSections = section.prerequisiteSections.ToDictionary(s => s.identifier, s => true);
    DeliveryPhases = section.deliveryPhases.Select(dp => new SerializedDeliveryPhase(dp));
    DisableFeaturesOnUnlock = section.disableFeaturesOnUnlock.ToDictionary(f => f.identifier, f => true);
    EnableFeaturesOnUnlock = section.enableFeaturesOnUnlock.ToDictionary(f => f.identifier, f => true);
    EnableFeaturesOnAvailable = section.enableFeaturesOnAvailable.ToDictionary(f => f.identifier, f => true);
  }

  public void Destroy(Section section)
  {
    // Remove from cache
    SectionCache.Instance.Remove(section.identifier);
    
    // Destroy GameObject
    GameObject.Destroy(section.gameObject);
  }

  internal void ApplyTo(Section section)
  {
    // Validate before applying
    Validate();
    Write(section);
  }
}
