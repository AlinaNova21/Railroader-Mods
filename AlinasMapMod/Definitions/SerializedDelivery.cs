using System.Linq;
using AlinasMapMod.Caches;
using AlinasMapMod.Validation;
using Game.Progression;
using Model.Ops;
using Model.Ops.Definition;

namespace AlinasMapMod.Definitions;

public class SerializedDelivery : IValidatable, ISerializedPatchable<Section.Delivery>
{
  public int Direction { get; set; } = 0;
  public int Count { get; set; } = 0;
  public string Load { get; set; } = "";
  public string CarTypeFilter { get; set; } = "";

  public SerializedDelivery()
  {
  }
  public SerializedDelivery(Section.Delivery d)
  {
    Read(d);
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
    return new ValidationResultCombiner()
      .Add(new ValidationBuilder<string>(nameof(Load))
        .Required()
        .ExistsInCache<Load>(key => LoadCache.Instance.TryGetValue(key, out _), "Load"), Load)
      .Add(new ValidationBuilder<int>(nameof(Direction))
        .AsValidEnum<Section.Delivery.Direction>(), Direction)
      .Add(new ValidationBuilder<int>(nameof(Count))
        .GreaterThan(0), Count)
      .Result;
  }

  public void Write(Section.Delivery delivery)
  {
    delivery.direction = (Section.Delivery.Direction)Direction;
    delivery.count = Count;
    
    // We know this will succeed because validation passed
    LoadCache.Instance.TryGetValue(Load, out var load);
    delivery.load = load;
    delivery.carTypeFilter = new CarTypeFilter(CarTypeFilter);
  }

  public void Read(Section.Delivery delivery)
  {
    Direction = (int)delivery.direction;
    Count = delivery.count;
    Load = delivery.load.id;
    CarTypeFilter = delivery.carTypeFilter.queryString;
  }

  internal void ApplyTo(Section.Delivery delivery)
  {
    // Validate before applying
    Validate();
    Write(delivery);
  }
}
