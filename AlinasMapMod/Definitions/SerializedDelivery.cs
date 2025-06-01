using AlinasMapMod.Caches;
using Game.Progression;
using Model.Ops;

namespace AlinasMapMod.Definitions;

public class SerializedDelivery
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
    Direction = (int)d.direction;
    Count = d.count;
    Load = d.load.id;
    CarTypeFilter = d.carTypeFilter.queryString;
  }

  internal void ApplyTo(Section.Delivery delivery)
  {
    delivery.direction = (Section.Delivery.Direction)Direction;
    delivery.count = Count;
    if (!LoadCache.Instance.TryGetValue(Load, out var load)) {
      throw new ValidationException($"Load '{Load}' not found.");
    }
    delivery.load = load;
    delivery.carTypeFilter = new CarTypeFilter(CarTypeFilter);
  }
}
