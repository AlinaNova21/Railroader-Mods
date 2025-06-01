using AlinasMapMod.Loaders;

namespace AlinasMapMod.Caches;

public class LoaderCache : ComponentCache<LoaderCache, LoaderInstance>
{
  public override string GetIdentifier(LoaderInstance obj) => obj.identifier;
}
