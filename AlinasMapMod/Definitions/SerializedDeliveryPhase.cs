using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Caches;
using Game.Progression;
using Model.Ops;

namespace AlinasMapMod.Definitions;

public class SerializedDeliveryPhase
{

  public IEnumerable<SerializedDelivery> Deliveries { get; set; } = new List<SerializedDelivery>();
  public int Cost { get; set; } = 0;
  public string IndustryComponent { get; set; } = "";

  public SerializedDeliveryPhase()
  {
  }

  public SerializedDeliveryPhase(Section.DeliveryPhase dp)
  {
    Deliveries = dp.deliveries.Select(d => new SerializedDelivery(d));
    Cost = dp.cost;
    IndustryComponent = dp.industryComponent?.Identifier ?? "";
  }

  internal void ApplyTo(Section.DeliveryPhase phase)
  {
    if (Deliveries.Count() > 0) {
      if (!IndustryComponentCache.Instance.TryGetValue(IndustryComponent, out var industryComponent)) {
        throw new ValidationException($"Industry component '{IndustryComponent}' not found.");
      }
      if (industryComponent is ProgressionIndustryComponent prog) {
        phase.industryComponent = prog;
      } else {
        throw new ValidationException($"Industry component '{IndustryComponent}' is not a progression industry component.");
      }
    }
    phase.cost = Cost;
    phase.deliveries = Deliveries.Select(d => {
      var delivery = new Section.Delivery();
      try {
        d.ApplyTo(delivery);
      } catch (ValidationException e) {
        throw new ValidationException($"Error applying delivery: {e.Message}", e);
      }
      return delivery;
    }).ToArray();
  }
}
