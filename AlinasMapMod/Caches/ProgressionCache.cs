using Game.Progression;

namespace AlinasMapMod.Caches;

public class ProgressionCache : ComponentCache<ProgressionCache, Progression>
{
  public override string GetIdentifier(Progression obj) => obj.identifier;
}
