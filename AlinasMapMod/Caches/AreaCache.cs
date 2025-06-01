using Model.Ops;

namespace AlinasMapMod.Caches;

public class AreaCache : ComponentCache<AreaCache, Area>
{
  public override string GetIdentifier(Area obj) => obj.identifier;
}
