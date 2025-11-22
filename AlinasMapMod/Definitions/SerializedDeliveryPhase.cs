using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Caches;
using AlinasMapMod.Validation;
using Game.Progression;
using Model.Ops;

namespace AlinasMapMod.Definitions;

public class SerializedDeliveryPhase : IValidatable, ISerializedPatchable<Section.DeliveryPhase>
{

  public IEnumerable<SerializedDelivery> Deliveries { get; set; } = new List<SerializedDelivery>();
  public int Cost { get; set; } = 0;
  public string IndustryComponent { get; set; } = "";

  public SerializedDeliveryPhase()
  {
  }

  public SerializedDeliveryPhase(Section.DeliveryPhase dp)
  {
    Read(dp);
  }

  public void Validate()
  {
    var result = ValidateWithDetails();
    if (!result.IsValid)
    {
      var firstError = result.Errors.FirstOrDefault();
      throw new ValidationException(firstError?.Message ?? "Validation failed");
    }
  }

  public ValidationResult ValidateWithDetails()
  {
    var combiner = new ValidationResultCombiner()
      .Add(new ValidationBuilder<int>(nameof(Cost))
        .GreaterThanOrEqual(0), Cost);

    // Validate IndustryComponent if deliveries exist
    if (Deliveries.Any())
    {
      combiner.Add(new ValidationBuilder<string>(nameof(IndustryComponent))
        .Required()
        .ExistsInCache<IndustryComponent>(key => IndustryComponentCache.Instance.TryGetValue(key, out _), "IndustryComponent")
        .OfCacheType<ProgressionIndustryComponent>(
          key => IndustryComponentCache.Instance.TryGetValue(key, out var component) ? component : null, 
          "IndustryComponent"), IndustryComponent);
    }

    // Validate each delivery
    foreach (var delivery in Deliveries)
    {
      combiner.Add(delivery.ValidateWithDetails());
    }

    return combiner.Result;
  }

  public void Write(Section.DeliveryPhase phase)
  {
    if (Deliveries.Any())
    {
      // We know this will succeed because validation passed
      IndustryComponentCache.Instance.TryGetValue(IndustryComponent, out var industryComponent);
      phase.industryComponent = industryComponent as ProgressionIndustryComponent;
    }

    phase.cost = Cost;
    phase.deliveries = Deliveries.Select(d => {
      var delivery = new Section.Delivery();
      d.Write(delivery); // Use new Write method
      return delivery;
    }).ToArray();
  }

  public void Read(Section.DeliveryPhase phase)
  {
    Deliveries = phase.deliveries.Select(d => new SerializedDelivery(d));
    Cost = phase.cost;
    IndustryComponent = phase.industryComponent?.Identifier ?? "";
  }

  internal void ApplyTo(Section.DeliveryPhase phase)
  {
    // Validate before applying
    Validate();
    Write(phase);
  }
}
