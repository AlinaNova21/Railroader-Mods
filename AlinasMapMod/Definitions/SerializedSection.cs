using System.Collections.Generic;
using System.Linq;
using Game.Progression;

namespace AlinasMapMod.Definitions;

public class SerializedSection
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
    DisplayName = s.displayName;
    Description = s.description;
    PrerequisiteSections = s.prerequisiteSections.ToDictionary(s => s.identifier, s => true);
    DeliveryPhases = s.deliveryPhases.Select(dp => new SerializedDeliveryPhase(dp));
    DisableFeaturesOnUnlock = s.disableFeaturesOnUnlock.ToDictionary(f => f.identifier, f => true);
    EnableFeaturesOnUnlock = s.enableFeaturesOnUnlock.ToDictionary(f => f.identifier, f => true);
    EnableFeaturesOnAvailable = s.enableFeaturesOnAvailable.ToDictionary(f => f.identifier, f => true);
  }

  internal void ApplyTo(Section sec)
  {
    sec.displayName = DisplayName;
    sec.description = Description;
    sec.prerequisiteSections = DefinitionUtils.ApplyList(sec.prerequisiteSections ?? [], PrerequisiteSections);
    sec.disableFeaturesOnUnlock = DefinitionUtils.ApplyList(sec.disableFeaturesOnUnlock ?? [], DisableFeaturesOnUnlock);
    sec.enableFeaturesOnUnlock = DefinitionUtils.ApplyList(sec.enableFeaturesOnUnlock ?? [], EnableFeaturesOnUnlock);
    sec.enableFeaturesOnAvailable = DefinitionUtils.ApplyList(sec.enableFeaturesOnAvailable ?? [], EnableFeaturesOnAvailable);

    sec.deliveryPhases = DeliveryPhases.Select(dp => {
      var phase = new Section.DeliveryPhase();
      try {
        dp.ApplyTo(phase);
      } catch (ValidationException e) {
        throw new ValidationException($"Error applying delivery phase: {e.Message}", e);
      }
      return phase;
    }).ToArray();
  }
}
