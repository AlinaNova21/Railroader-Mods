using Model.Ops;

namespace AlinasMapMod.Caches;

public class IndustryCache : ComponentCache<IndustryCache, Industry>
{
  public override string GetIdentifier(Industry obj) => obj.identifier;
}
