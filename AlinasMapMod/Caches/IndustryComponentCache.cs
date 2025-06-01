using Model.Ops;

namespace AlinasMapMod.Caches;

public class IndustryComponentCache : ComponentCache<IndustryComponentCache, IndustryComponent>
{
  public override string GetIdentifier(IndustryComponent obj) => obj.Identifier;
}
