using UI.Map;

namespace AlinasMapMod.Caches;

public class MapLabelCache : ComponentCache<MapLabelCache, MapLabel>
{
  public override string GetIdentifier(MapLabel obj) => obj.name;
}
